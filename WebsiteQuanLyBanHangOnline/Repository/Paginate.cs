namespace WebsiteQuanLyBanHangOnline.Repository
{
    public class Paginate
    {
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }

        public Paginate() { }

        public Paginate(int totalItems, int page, int pageSize = 10)
        {
            int totalPages = (int)Math.Ceiling((decimal)totalItems / pageSize);
            int currentPage = page;

            if (currentPage < 1)
                currentPage = 1;
            if (currentPage > totalPages)
                currentPage = totalPages;

            int startPage = currentPage - 5;
            int endPage = currentPage + 4;

            if (startPage < 1)
            {
                endPage += 1 - startPage;
                startPage = 1;
            }

            if (endPage > totalPages)
            {
                endPage = totalPages;
            }

            TotalItems = totalItems;
            PageSize = pageSize;
            CurrentPage = currentPage;
            TotalPages = totalPages;
            StartPage = startPage;
            EndPage = endPage;
        }
    }
}
