// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Common

module Utils =
    open MongoDB.Bson
    open MongoDB.Driver
    open MongoDB.FSharp

    [<Literal>]
    let ConnectionString =
        "mongodb://mongdb4proj4:VDPdRvJiVDPN0VFZtllAXzmhz496N2uV5owy52oct5PxjYjg4zp48JLA7W0HFixTNi8bZt6RnpEXObLdCdfEZw==@mongdb4proj4.mongo.cosmos.azure.com:10255/?ssl=true&retrywrites=false&replicaSet=globaldb&maxIdleTimeMS=120000&appName=@mongdb4proj4@"

    let localConnectionString = "mongodb://localhost:27017"

    [<Literal>]
    let DbName = "project4"

    let P4GetCollection<'a> (cName: string) =


        // let sslSettings = SslSettings()
        // sslSettings.EnabledSslProtocols <- SslProtocols.Tls12

        // let mongoClientSettings =
        //     MongoClientSettings.FromConnectionString ConnectionString

        // mongoClientSettings.SslSettings <- sslSettings

        let client = MongoClient(ConnectionString)
        //let client = MongoClient(localConnectionString)
        let db = client.GetDatabase(DbName)
        db.GetCollection<'a>(cName)


    // https://github.com/rikace/akkafractal/blob/master/src/Akka.Fractal.Common/AkkaHelpers.fs
    module ConfigurationLoader =
        open System.IO
        open Akka.Configuration

        let loadConfig (configFile: string) =
            if File.Exists(configFile) |> not
            then raise (FileNotFoundException(sprintf "Cannot find akka config file %s" configFile))

            let config = File.ReadAllText(configFile)
            ConfigurationFactory.ParseString(config)

        let load () = loadConfig ("akka.conf")

    let extractText (text: string) (startWith: char) =
        let mutable sIdx = 0
        let mutable eIdx = 0
        let mutable inET = false
        let mutable et = []
        let startChar: char = startWith
        let emptyChar: char = ' '
        let sharpChar: char = '#'
        let atChar: char = '@'

        let appendEt () =
            let t = (text.[sIdx..eIdx], (sIdx, eIdx))
            et <- t :: et

        text
        |> Seq.iteri (fun i c ->
            if inET then
                if (c = emptyChar) || (c = sharpChar) || (c = atChar) then
                    inET <- false
                    eIdx <- (i - 1)
                    appendEt ()
                    if c = startChar then
                        inET <- true
                        sIdx <- i
                else
                    ()
            else if c = startChar then
                inET <- true
                sIdx <- i
            else
                ())

        if inET then
            eIdx <- text.Length - 1
            appendEt ()
        else
            ()
        et
