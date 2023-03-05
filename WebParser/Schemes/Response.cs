using System.Collections.Generic;

namespace WebParser.Schemes;

public class Response
{
    public Data Data { get; set; }
}

public class Data
{
    public SearchReportWoodDeal SearchReportWoodDeal { get; set; }
}

public class SearchReportWoodDeal
{
    public List<Deal> Content { get; set; }
}