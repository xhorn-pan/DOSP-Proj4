// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils

let echoServer sys = 
    spawn sys "EchoServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                let sender = mailbox.Sender()
                match box message with
                | :? string -> 
                        printfn "super!"
                        sender <! sprintf "Hello %s remote" message
                        return! loop()
                | _ ->  failwith "unknown message"
            } 
        loop()

[<EntryPoint>]
let main argv =
    let config = ConfigurationLoader.load()
    let system = ActorSystem.Create("clusterT", config)
    echoServer system |> ignore

    system.WhenTerminated.Wait()
    0 // return an integer exit code