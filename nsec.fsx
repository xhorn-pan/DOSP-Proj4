#r "nuget: NSec.Cryptography"

open NSec.Cryptography

let alg = SignatureAlgorithm.Ed25519
let keyAll = Key.Create alg

if keyAll.HasPublicKey then
    let pub =
        keyAll.PublicKey.Export KeyBlobFormat.PkixPrivateKey

    printfn "%A" (pub |> System.Text.Encoding.Default.GetString)
