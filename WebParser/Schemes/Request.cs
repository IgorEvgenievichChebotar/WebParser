namespace WebParser.Schemes;

public class Request
{
    public string query { get; set; }
    public Variables variables { get; set; }
    public string operationName { get; set; }

    public Request(int size, int number)
    {
        query =
            "query SearchReportWoodDeal($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) {  searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) {    content {      sellerName      sellerInn      buyerName      buyerInn      woodVolumeBuyer      woodVolumeSeller      dealDate      dealNumber      __typename    }    __typename  }}";
        variables = new Variables
        {
            size = size,
            number = number,
            filter = null,
            orders = null
        };
        operationName = "SearchReportWoodDeal";
    }

    public class Variables
    {
        public int size { get; set; }
        public int number { get; set; }
        public object filter { get; set; }
        public object orders { get; set; }
    }
}