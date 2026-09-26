namespace TuyenXe.DTOs
{
    public class TuyenXeUpdateDto
    {
        public string TenTuyen { get; set; } = string.Empty;

        public int MaDiemDi { get; set; }

        public int MaDiemDen { get; set; }

        public decimal? KhoangCach { get; set; }

        public int? ThoiGianDuKien { get; set; }

        public bool TrangThai { get; set; }
    }
}