// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module EngineResp =
    open WebSharper

    [<JavaScript; NamedUnionCases "resp_type">]
    type RespType =
        | [<Constant "success">] Succ
        | [<Constant "failed">] Fail

    type EngineResp<'a> = { RType: RespType; Body: 'a }

    let RespSucc (msg: 'a) = { RType = Succ; Body = msg }
    let RespFail (msg: 'a) = { RType = Fail; Body = msg }
