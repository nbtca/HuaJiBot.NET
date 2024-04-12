#r "nuget: SharpCompress, 0.36.0"
open System.IO
let isWindows = System.OperatingSystem.IsWindows()
let isDebugBuild = isWindows
let run cmd args =
    printfn "Exec %s %A" cmd args
    let psi = cmd |> System.Diagnostics.ProcessStartInfo
    for arg in args do
        psi.ArgumentList.Add(arg)
    psi.UseShellExecute <- isWindows
    psi.CreateNoWindow <- false
    let p = System.Diagnostics.Process.Start(psi)
    p.WaitForExit()
    p.ExitCode
let cd = __SOURCE_DIRECTORY__
let src = Path.Combine(cd, "src")
let output = Path.Combine(cd, "bin")
let dotnetBuild src output = 
    let extraArgs = if isDebugBuild then ["-c"; "Debug"] else []
    let exitCode = run "dotnet" (["build"; src; "-o"; output]@extraArgs)
    if exitCode <> 0 then failwithf "Build failed for %s" src
//delete output folder
if output|>Directory.Exists then
    Directory.Delete(output,true)
let projs = Directory.GetFiles(src, "*.csproj", SearchOption.AllDirectories)
            |> Array.map (fun p -> Path.GetRelativePath(cd,p))
let coreFileNameList=
    let projName = "HuaJiBot.NET"
    let outputDir = Path.Combine(output, projName)
    //run "dotnet" ["build"; Path.Combine("src",projName,"HuaJiBot.NET.csproj"); "-o"; outputDir]
    dotnetBuild (Path.Combine("src",projName,"HuaJiBot.NET.csproj")) outputDir
    let fileNameList=
        outputDir
        |> Directory.GetFiles
        |> Array.map (fun p -> Path.GetFileName(p))
    Directory.Delete(outputDir,true)
    fileNameList

let buidActions = [ 
    for proj in projs do
        printfn "Processing %s" proj
        let filename = Path.GetFileName(proj)
        if filename.StartsWith("HuaJiBot.NET.Plugin") then
            async {
                let projName = Path.GetFileNameWithoutExtension(proj)
                let outputDir = Path.Combine(output, projName)
                if outputDir|>Directory.Exists|>not then outputDir|>Directory.CreateDirectory|>ignore
                printfn "Building %s" filename
                //let exitCode = run "dotnet" ["build"; proj; "-o"; outputDir]
                dotnetBuild proj outputDir
                //delete files which already included in executable and no need to copy as dependency
                for file in coreFileNameList do
                    let coreFile = Path.Combine(outputDir, file)
                    if coreFile|>File.Exists then
                        File.Delete(coreFile)
                        printfn "Delete %s" coreFile
                printfn "Build %s done" filename
                return outputDir
            }
    ]
let binDirs=
    if isWindows then
        [for dir in buidActions|>Async.Parallel|>Async.RunSynchronously do dir]
    else 
        [for action in buidActions do action|>Async.RunSynchronously]
//resort files
let pluginDir = Path.Combine(output, "plugins")
if pluginDir|>Directory.Exists|>not then pluginDir|>Directory.CreateDirectory|>ignore
let libsDir = Path.Combine(pluginDir, "libs")
if libsDir|>Directory.Exists|>not then libsDir|>Directory.CreateDirectory|>ignore
for dir in binDirs do
    let files = Array.concat [Directory.GetFiles(dir,"*.dll")  ;Directory.GetFiles(dir,"*.pdb")]
    for file in files do
        let fileName = Path.GetFileName(file)
        if fileName.StartsWith("HuaJiBot.NET.Plugin") then
            let dest = Path.Combine(pluginDir, fileName)
            File.Copy(file, dest, true)
            printfn "Copy %s to %s" file dest
        else
            let dest = Path.Combine(libsDir, fileName)
            File.Copy(file, dest, true)
            printfn "Copy %s to %s" file dest
//remove original output
for dir in binDirs do
    Directory.Delete(dir,true)
    printfn "Delete %s" dir
//compress
open SharpCompress.Archives
open SharpCompress.Archives.Zip
let compress()=
    let zipFile = Path.Combine(cd, "HuaJiBot.NET.Plugins.zip")
    if zipFile|>File.Exists then File.Delete(zipFile)
    use archive = ZipArchive.Create()
    archive.AddAllFromDirectory output
    use fileStream = File.Create(zipFile)
    archive.SaveTo(fileStream)|>ignore
if isWindows then
    compress()