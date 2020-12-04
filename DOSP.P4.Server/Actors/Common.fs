// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors



module Common =
    open System
    open System.Security.Authentication

    open Akka.Actor
    open Akka.FSharp
    open Akka.DistributedData
    open MongoDB.Bson
    open MongoDB.Driver
    open MongoDB.FSharp

    type DBPut = DBPut of Async<IUpdateResponse> * IActorRef
    type DBGet = DBGet of Async<IGetResponse> * IKey * IActorRef

    let rcPolicy = ReadMajority(TimeSpan.FromSeconds 3.)
    let readLocal = ReadLocal.Instance
    let wcPolicy = WriteMajority(TimeSpan.FromSeconds 3.)
    let writeLocal = WriteLocal.Instance

    let getChildActor name cActor (mailbox: Actor<_>) =
        let aRef = mailbox.Context.Child(name)
        if aRef.IsNobody() then spawn mailbox name cActor else aRef

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
