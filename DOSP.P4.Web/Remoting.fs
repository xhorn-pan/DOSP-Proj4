namespace DOSP.P4.Web

open WebSharper

module Server =

    [<Rpc>]
    let UserReg (name: string) (pubKey: string) = ()

    [<Rpc>]
    let DoSomething input =
        let R (s: string) =
            System.String(Array.rev (s.ToCharArray()))

        async { return R input }
