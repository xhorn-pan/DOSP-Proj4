// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module EngineResp =
    type EngineResp =
        { ERType: bool
          Message: string }
        static member PositiveRespone(msg: string) = { ERType = true; Message = msg }
        static member NegtiveRespone(msg: string) = { ERType = false; Message = msg }
