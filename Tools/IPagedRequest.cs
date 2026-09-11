namespace OdinCore.Tools;

public interface IPagedRequest
{
    int? page { get; set; }
    int? pageSize { get; set; }
}