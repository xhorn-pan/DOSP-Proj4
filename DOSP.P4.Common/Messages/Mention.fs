namespace DOSP.P4.Common.Messages

open DOSP.P4.Common.Utils

module Mention =
    open User

    type Mention = { User: User; Indices: int * int }

    let GetMentions (text: string) =
        let ms = extractText text '@'
        ms
        |> List.map (fun (txt, se) ->
            let user = CreateUser 0L txt
            { User = user; Indices = se })
