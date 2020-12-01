// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Client

module Utils =
    open System
    open MathNet.Numerics.Distributions

    let a = 2. // zipf a param
    let getZipF (total: int) = Zipf(a, total)

    let getZipfFollower (uids: string list) =
        let uidsLen = uids.Length
        let zipf = getZipF uidsLen
        let rnd = Random()
        uids
        |> List.map (fun uid ->
            let numOfFollowers = zipf.Sample()

            let followers =
                [ 1 .. numOfFollowers ]
                |> List.map (fun idx -> List.item (rnd.Next(uidsLen)) uids)
                |> Set.ofList

            (uid, followers))
