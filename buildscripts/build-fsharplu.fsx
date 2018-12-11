#r "tools/FAKE/tools/FakeLib.dll"

open Fake
open Fake.DotNetCli

let buildDir  = "./build/"
let nugetToolPath = "../.nuget/nuget.exe"
let mainProjectName = "NRK.FSharpLu"
let jsonProjectName = "NRK.FSharpLu.Json"
let mainProjectToBuild = "./FSharpLu/FSharpLu.fsproj"
let jsonProjectToBuild = "./FSharpLu.Json/FSharpLu.Json.fsproj"
let testProject = "./FSharpLu.Tests/FSharpLu.Tests.fsproj"

let getTeamCityBuildNumberOrDefault() =
    match TeamCityHelper.TeamCityBuildNumber with
    | Some v -> v
    | None -> "1"

let isMasterBranch = TeamCityHelper.getTeamCityBranchIsDefault();

let nugetVersion = "0.0." + getTeamCityBuildNumberOrDefault()

let nugetSources = (environVarOrDefault "nuget.sources" "https://api.nuget.org/v3/index.json").Split([|','|]) 
                    |> Array.toList
                    |> List.map (fun source -> "-s " + source)

let version = 
  if isMasterBranch then
    environVarOrDefault "version" nugetVersion
  else
    environVarOrDefault "version" (nugetVersion + "-alpha")

Target "Clean" (fun _ ->
    CleanDirs [buildDir]
)

//Restore Nuget
Target "RestorePackages" (fun _ ->
        DotNetCli.Restore (fun p ->
        {p with
            AdditionalArgs = nugetSources
            WorkingDir = "../"})
)

//Build projects
Target "BuildProjects" (fun _ ->
    DotNetCli.Build
     (fun p -> 
       { p with 
            Configuration = "Release" 
            WorkingDir = "../"
            Project = mainProjectToBuild})
    DotNetCli.Build
     (fun p -> 
       { p with 
            Configuration = "Release" 
            WorkingDir = "../"
            Project = jsonProjectToBuild})
)

Target "BuildTests" (fun _ -> 
    DotNetCli.Build
     (fun p -> 
       { p with 
            Configuration = "Release" 
            WorkingDir = "../"
            Project = testProject}))

Target "Test" (fun _ ->
    DotNetCli.Test 
        (fun p ->
            {
              p with 
                  Configuration = "Release"
                  WorkingDir = "../"
                  Project = testProject})
)

Target "CreateNugetPackage" (fun _ ->
    DotNetCli.Pack (fun c ->
        { c with 
            Configuration = "Release"
            Project = mainProjectToBuild
            AdditionalArgs = [ "/p:PackageVersion=" + version; "/p:Version=" + version; ]           
            OutputPath = "../buildscripts/build"
            WorkingDir = "../"})
    DotNetCli.Pack (fun c ->
        { c with 
            Configuration = "Release"
            Project = jsonProjectToBuild
            AdditionalArgs = [ "/p:PackageVersion=" + version; "/p:Version=" + version; ]           
            OutputPath = "../buildscripts/build"
            WorkingDir = "../"})
)

Target "PublishNugetPackage" (fun _ ->
    SetBuildNumber version
    NuGetPublish (fun p ->
    {p with
        PublishUrl = environVarOrDefault "myget.publishUrl" ""
        AccessKey = environVarOrDefault "myget.accessKey" ""
        Project = mainProjectName
        WorkingDir = buildDir
        OutputPath = buildDir
        Version = version
        ToolPath = nugetToolPath
    })
    NuGetPublish (fun p ->
    {p with
        PublishUrl = environVarOrDefault "myget.publishUrl" ""
        AccessKey = environVarOrDefault "myget.accessKey" ""
        Project = jsonProjectName
        WorkingDir = buildDir
        OutputPath = buildDir
        Version = version
        ToolPath = nugetToolPath
    })
)

"Clean"
==> "RestorePackages"
==> "BuildProjects"
==> "BuildTests"
==> "Test"
==> "CreateNugetPackage"
==> "PublishNugetPackage"

RunTargetOrDefault "CreateNugetPackage"
