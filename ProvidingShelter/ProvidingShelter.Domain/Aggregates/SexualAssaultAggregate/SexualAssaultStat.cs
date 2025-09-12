using System;

namespace ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate
{
    public sealed class SexualAssaultStat
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ImportId { get; private set; }              // FK → SexualAssaultImport

        // 維度
        public int Year { get; private set; }
        public string CityCode { get; private set; } = default!; // 對應 RisCityCode.CityCode
        public string CityName { get; private set; } = default!; // 正規化後的顯示名稱（臺北市）

        public NationalityCode Nationality { get; private set; }
        public CrossCategoryType CategoryType { get; private set; }

        // 年齡或行業鍵（通用欄）
        public string CategoryKey { get; private set; } = default!;
        public string CategoryNameZh { get; private set; } = default!;

        // 衡量值
        public int Count { get; private set; }
        public bool IsTotalRow { get; private set; }

        // 追蹤
        public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

        public SexualAssaultStat(
            Guid importId,
            int year,
            string cityCode,
            string cityName,
            NationalityCode nationality,
            CrossCategoryType categoryType,
            string categoryKey,
            string categoryNameZh,
            int count,
            bool isTotalRow = false)
        {
            ImportId = importId;
            Year = year;
            CityCode = cityCode;
            CityName = cityName;
            Nationality = nationality;
            CategoryType = categoryType;
            CategoryKey = categoryKey;
            CategoryNameZh = categoryNameZh;
            Count = count;
            IsTotalRow = isTotalRow;
        }
    }
}
