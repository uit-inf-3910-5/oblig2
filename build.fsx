#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators

Target.initEnvironment ()

let serverPath = Path.getFullName "./src"
let clientPath = Path.getFullName "./src/Client"
let clientDeployPath = Path.combine clientPath "deploy"
let deployDir = Path.getFullName "./deploy"
let publicDir = Path.getFullName "./deploy/public"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
// let yarnTool = platformTool "yarn" "yarn.cmd"
let yarnTool = platformTool "echo" "echo"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore

let commitId () =
    ShellCommand "git rev-parse --short HEAD"
    |> CreateProcess.fromCommand
    |> CreateProcess.redirectOutput
    |> Proc.run
    |> fun result ->
        if result.ExitCode <> 0
            then "unknown"
        else
            result.Result.Output

let writeVersionFile file =
    let commit = commitId ()
    let json =
        sprintf """
            {
                commit: %s,
                version: %s
            }
        """ commit release.NugetVersion
    File.writeString false file json

Target.create "Clean" (fun _ ->
    [
      deployDir
      // clientDeployPath
    ]
    |> Shell.cleanDirs
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
)

Target.create "Build" (fun _ ->
    // writeVersionFile "src/Server/version.json"
    runDotNet "build" serverPath
    runTool yarnTool "webpack-cli -p" __SOURCE_DIRECTORY__
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runTool yarnTool "webpack-dev-server" __SOURCE_DIRECTORY__
        // runTool yarnTool "webpack -d --watch" __SOURCE_DIRECTORY__
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "http://localhost:8080"
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [ if not safeClientOnly then yield server
          yield client
          if not vsCodeSession then yield browser ]

    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

let webpackCli = sprintf "webpack-cli -p --output-path %s" publicDir

let copyExamples () =
    !! "src/*.csv"
    |> GlobbingPattern.setBaseDir "src"
    |> Shell.copyFilesWithSubFolder "deploy"

let deploy release =
    let dotnetWorkDir = DotNet.Options.withWorkingDirectory serverPath
    let opts = sprintf "-c %s -o %s" release deployDir
    let result = DotNet.exec dotnetWorkDir "publish" opts
    if result.ExitCode <> 0 then
        failwithf "'dotnet publish' failed in %s" serverPath
    else
        runTool yarnTool webpackCli __SOURCE_DIRECTORY__
    copyExamples ()

Target.create "Release" (fun _ -> deploy "Release")

Target.create "Debug" (fun _ -> deploy "Debug")

open Fake.Core.TargetOperators

"Clean"
    ==> "InstallClient"
    ==> "Build"

"Clean"
    ==> "InstallClient"
    ==> "Run"

"Clean"
    ==> "InstallClient"
    ==> "Release"

"Clean"
    ==> "InstallClient"
    ==> "Debug"

Target.runOrDefaultWithArguments "Build"
