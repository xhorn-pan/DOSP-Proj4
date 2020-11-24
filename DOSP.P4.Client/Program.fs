// Name: Xinghua Pan
// UFID: 95160902

open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils
open DOSP.P4.Common.Messages
open DOSP.P4.Client.Actors
[<EntryPoint>]
let main argv =
    let config = ConfigurationLoader.load()
    let system = ActorSystem.Create("project4", config)
    let client = spawn system "client" ClientActor
    
    let u1 = CreateUser 1014837012L "Sergey Mutin"
    
    System.Threading.Thread.Sleep 2000
    client <! RegisterUser u1
    
    system.WhenTerminated.Wait()
    0 // return an integer exit code