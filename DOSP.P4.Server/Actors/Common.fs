// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors

module Common =
    open System
    open Akka.Actor
    open Akka.FSharp
    open Akka.DistributedData

    type DBPut = DBPut of Async<IUpdateResponse> * IActorRef
    type DBGet = DBGet of Async<IGetResponse> * IKey * IActorRef

    let rcPolicy = ReadMajority(TimeSpan.FromSeconds 3.)
    let readLocal = ReadLocal.Instance
    let wcPolicy = WriteMajority(TimeSpan.FromSeconds 3.)
    let writeLocal = WriteLocal.Instance

    let getChildActor name cActor (mailbox: Actor<_>) =
        let aRef = mailbox.Context.Child(name)
        if aRef.IsNobody() then spawn mailbox name cActor else aRef
