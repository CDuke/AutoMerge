#r @"packages/FAKE.2.16.1/tools/FakeLib.dll"
open Fake
open System.Xml

RestorePackages()

// Properties
let vsVersion = getBuildParamOrDefault "vsversion" "2012"
let buildDirBase = "./bin/"
let buildDir = buildDirBase + (if vsVersion = "2012" then "/2012" else "/2013")
let vsixContentDir = buildDir + "/__vsixContent"
let manifestPath = vsixContentDir + "/extension.vsixmanifest"
let vsixFile = buildDir + "/AutoMerge.vsix"


// files
let slnReferences = !! (if vsVersion = "2012" then "AutoMerge_VS2012.sln" else "AutoMerge_VS2013.sln")

let UpdateVsixManifest _=
    Unzip vsixContentDir vsixFile
    let namespaces = [("vsix", "http://schemas.microsoft.com/developer/vsx-schema/2011")]
    let doc = new XmlDocument()
    doc.Load manifestPath
    let nsmgr = XmlNamespaceManager(doc.NameTable)
    namespaces |> Seq.iter nsmgr.AddNamespace
    //Version
    let identityNode = doc.SelectSingleNode("/vsix:PackageManifest/vsix:Metadata/vsix:Identity", nsmgr)
    let currentVersion = identityNode.Attributes.["Id"].Value
    (identityNode.Attributes.["Id"]).Value <- "AutoMerge.VS" + vsVersion + "." + currentVersion
    //Name
    let displayNameNode = doc.SelectSingleNode("/vsix:PackageManifest/vsix:Metadata/vsix:DisplayName", nsmgr)
    displayNameNode.InnerText <- displayNameNode.InnerText + " for Visual Studio " + vsVersion
    doc.Save manifestPath
    Zip vsixContentDir vsixFile !!(vsixContentDir + "/**/*.*")

// Targets
Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "UpdateVsixManifest"(fun _ ->
    UpdateVsixManifest 1
)

Target "BuildApp" (fun _ ->
    slnReferences
        |> MSBuildRelease  buildDir "Build"
        |> Log "AppBuild-Output: "
)

Target "Deploy" DoNothing

Target "All" DoNothing


// Dependencies
"Clean"
    ==> "BuildApp"
    ==> "UpdateVsixManifest"
    ==> "Deploy"
    ==> "All"

// start build
RunTargetOrDefault "All"