namespace ProvidingShelter.Domain.Entities
{
    public class SexualAssaultInformation
    {
        public string? OwnerCityCode { get; set; }
        public string? InfoerType { get; set; }
        public string? InfoUnit { get; set; }
        public string? OtherInfoerType { get; set; }
        public string? OtherInfoUnit { get; set; }
        public string? ClientId { get; set; }
        public string? Gender { get; set; }
        public int? BDate { get; set; }
        public string? IdType { get; set; }
        public string? Occupation { get; set; }
        public string? OtherOccupation { get; set; }
        public string? Education { get; set; }
        public string? Maimed { get; set; }
        public string? OtherMaimed { get; set; }
        public string? OtherMaimed2 { get; set; }
        public string? School { get; set; }
        public string? DId { get; set; }
        public string? DSexId { get; set; }
        public int? DBDate { get; set; }
        public byte? NumOfSuspect { get; set; }
        public string? Relation { get; set; }
        public string? OtherRelation { get; set; }
        public string? OccurCity { get; set; }
        public string? OccurPlace { get; set; }
        public string? OccurTown { get; set; }
        public string? OtherOccurPlace { get; set; }
        public DateTime? LastOccurTime { get; set; }

        public short? InfoTimeYear { get; set; }
        public byte? InfoTimeMonth { get; set; }
        public string? TownCode { get; set; }

        // 受理時間與通報日數差
        public DateTime? ReceiveTime { get; set; }
        public int? NotifyDate { get; set; }
    }
}
