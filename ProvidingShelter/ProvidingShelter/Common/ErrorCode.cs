namespace ProvidingShelter.Common
{
    public class ErrorCode
    {
        //For MediaCenter
        public const int KErrNone = 0;
        public const int KErrDBError = -1;
        public const int KErrErrorStatus = -2;
        //表示整合（與第三方 API）過程中的問題。
        public const int KErrIntegrationException = -3;
        public const int KErrJsonParseError = -4;

        // identifier for the event.
        public const int KEventHTTP = 1;

        /// <summary>
        /// 用於頁面上的執行結果訊息
        /// </summary>
        public enum ReturnCode
        {
            None = 0
            , AuthorizationFailed = 1
            , ParamError = 2 //參數錯誤
            , DBConnectError = 3 //DB連線失敗
            , OperationFailed = 4 //操作失敗
            , OperationSuccessful = 5 //操作成功
            , DataNotFound = 6 //使用的ID找不到資料
            , ParseError = 7
            , IntegrationException = 8
            , IntegrationHTTP = 9
            , JsonParseError = 10
        }
    }
}
