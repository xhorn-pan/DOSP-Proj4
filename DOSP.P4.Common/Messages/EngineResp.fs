// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

open WebSharper

module EngineResp =
    type RespType =
        | Succ
        | Fail


    type EngineResp<'a> =
        { [<Name "resp-type">]
          RType: RespType
          [<Name "resp-body">]
          Body: 'a }

    let RespSucc (msg: 'a) = { RType = Succ; Body = msg }
    let RespFail (msg: 'a) = { RType = Fail; Body = msg }
