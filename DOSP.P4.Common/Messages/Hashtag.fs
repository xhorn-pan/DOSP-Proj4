// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

open DOSP.P4.Common.Utils

module HashTag =
    type HashTag =
        { Text: string
          Indices: int * int }
        static member GetHashTags(text: string) =
            let tags = extractText text '#'
            tags
            |> List.map (fun (txt, se) -> { Text = txt; Indices = se })
