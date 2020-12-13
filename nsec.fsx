#r "nuget: NSec.Cryptography"

open NSec.Cryptography

let alg = SignatureAlgorithm.Ed25519
let keyAll = Key.Create alg

if keyAll.HasPublicKey then
    let pub =
        keyAll.PublicKey.Export KeyBlobFormat.PkixPublicKeyText

    printfn "%A" (pub |> System.Text.Encoding.Default.GetString)

open System
// hex string to byte
let fromHex (s: string) =
    s
    |> Seq.windowed 2
    |> Seq.mapi (fun i j -> (i, j))
    |> Seq.filter (fun (i, j) -> i % 2 = 0)
    |> Seq.map (fun (_, j) -> Byte.Parse(String(j), Globalization.NumberStyles.AllowHexSpecifier))
    |> Array.ofSeq
// import publickey
//PublicKey.Import(alg, ReadOnlySpan<byte>(hk), KeyBlobFormat.RawPublicKey);;
