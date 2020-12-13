// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors



module Common =
    open Akka.Actor
    open Akka.FSharp

    let getChildActor name cActor (mailbox: Actor<_>) =
        let aRef = mailbox.Context.Child(name)
        if aRef.IsNobody() then spawn mailbox name cActor else aRef
