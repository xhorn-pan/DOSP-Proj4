namespace DOSP.P4.Common.Messages

open DOSP.P4.Common.Utils

module Mention =
    open User

    type Mention = { User: User; Indices: int * int }

// let GetMentions (text: string) =
//     let ms = extractText text '@'
//     ms
//     |> List.map (fun (txt, se) ->
//         let user = CreateUser txt
//         { User = user; Indices = se })
