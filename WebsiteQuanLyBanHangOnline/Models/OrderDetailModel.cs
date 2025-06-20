﻿using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class OrderDetailModel
    {
        public int Id { get; set; }
        public string OrderCode { get; set; }
        public string UserName { get; set; }    
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public ProductModel Product { get; set; }
    }
}
