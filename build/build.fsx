#r @"../tools/FAKE/tools/FakeLib.dll"
open Fake
open System.Xml

// Properties
let vsVersion = environVarOrDefault "VisualStudioVersion" "12.0"
let buildDirBase = "./bin/"
let buildDir = buildDirBase + vsVersion


// files
let slnReferences = !!"./src/AutoMerge.sln"

// Targets
Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (fun id -> (RestorePackage (fun p -> {p with OutputPath = "./lib"}) id))
)

Target "BuildApp" (fun _ ->
    slnReferences
        |> MSBuildRelease  buildDir "Build"
        |> Log "AppBuild-Output: "
)

Target "All" DoNothing


// Dependencies
"Clean"
    ==> "RestorePackages"
    ==> "BuildApp"
    ==> "All"

// start build
RunTargetOrDefault "All"