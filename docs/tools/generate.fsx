// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries = [ "UkModulusCheck.dll" ]

let githubLink = "http://github.com/transactiveltd/uk-modulus-checking"

// Specify more information about your project
let info =
  [ "project-name", "UKModulusCheck"
    "project-author", "Transactive Ltd"
    "project-summary", "Validate UK sort code / account number using algorithms and weightings published by VocaLink."
    "project-github", githubLink
    "project-nuget", "http://nuget.org/packages/UkModulusCheck" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#load "../../packages/build/FSharp.Formatting/FSharp.Formatting.fsx"
#r "../../packages/build/FAKE/tools/NuGet.Core.dll"
#r "../../packages/build/FAKE/tools/FakeLib.dll"
open Fake
open System.IO
open FSharp.Formatting.Razor

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ @@ "../../src/UkModulusCheck/bin/Release/net461/"
let content    = __SOURCE_DIRECTORY__ @@ "../content"
let output     = __SOURCE_DIRECTORY__ @@ "../output"
let files      = __SOURCE_DIRECTORY__ @@ "../files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/build/FSharp.Formatting/"
let docTemplate = formatting @@ "templates/docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRootsAll = new System.Collections.Generic.Dictionary<string, string list>()
layoutRootsAll.Add("en", [ templates; formatting @@ "templates"; formatting @@ "templates/reference" ])
subDirectories (directoryInfo templates)
|> Seq.iter (fun d ->
    let name = d.Name
    if name.Length = 2 || name.Length = 3 then
        layoutRootsAll.Add(name, [templates @@ name; formatting @@ "templates"; formatting @@ "templates/reference" ]))

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
    CopyRecursive files output true |> Log "Copying file: "
    ensureDirectory (output @@ "content")
    CopyRecursive (formatting @@ "styles") (output @@ "content") true |> Log "Copying styles and scripts: "

// Build API reference from XML comments
let buildReference () =
    CleanDir (output @@ "reference")
    let binaries =
        referenceBinaries
        |> List.map (fun lib -> bin @@ lib)
    RazorMetadataFormat.Generate(
        dllFiles = binaries,
        outDir = output @@ "reference",
        layoutRoots = layoutRootsAll.["en"],
        parameters = ("root", "../") :: info,
        sourceRepo = githubLink @@ "tree/master",
        sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
        publicOnly = true,
        libDirs = [bin],
        markDownComments = true
    )


// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
    let rec getRelativePath root initialSubDir subDir =
        let root' = Path.GetFullPath root
        let subDir' = Path.GetFullPath subDir
        if subDir' = Path.GetPathRoot subDir' then failwithf "Cannot find relative path for %s and %s" root initialSubDir
        if root' = subDir' then "."
        else
            let next = getRelativePath root' initialSubDir (Path.GetDirectoryName subDir')
            if next = "." then ".." else "../" + next

    let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
    for dir in Seq.append [content] subdirs do
        let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
        let langSpecificPath(lang, path:string) =
            path.Split([|'/'; '\\'|], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.exists(fun i -> i = lang)
        let layoutRoots =
            let key = layoutRootsAll.Keys |> Seq.tryFind (fun i -> langSpecificPath(i, dir))
            match key with
            | Some lang -> layoutRootsAll.[lang]
            | None -> layoutRootsAll.["en"] // "en" is the default language
        RazorLiterate.ProcessDirectory(
            inputDirectory = dir,
            templateFile = docTemplate,
            outputDirectory = output @@ sub,
            replacements = ("root", getRelativePath content dir dir)::info,
            layoutRoots = layoutRoots,
            generateAnchors = true,
            processRecursive = false
        )

// Generate
copyFiles()
#if HELP
buildDocumentation()
#endif
#if REFERENCE
buildReference()
#endif
