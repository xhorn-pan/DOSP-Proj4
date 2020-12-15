// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module EngineResp =

    type EngineRespSucc = EngineRespSucc of msg: string
    type EngineRespError = EngineRespError of msg: string
