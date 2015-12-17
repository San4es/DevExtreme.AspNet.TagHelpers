﻿using DevExtreme.AspNet.Data;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Newtonsoft.Json;
using Samples.Models.Northwind;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#warning TODO review LINQ queries below after the following EF7 bugs are fixed
// https://github.com/aspnet/EntityFramework/issues/2341 "Support translating of GroupBy() to SQL"
// https://github.com/aspnet/EntityFramework/issues/3626 "GroupBy throws NRE when grouping using navigation properties"
// https://github.com/aspnet/EntityFramework/issues/3674 "Grouping with Where on a navigation prop fails" 
// https://github.com/aspnet/EntityFramework/issues/3675 "Sequence contains more than one element when grouping by a nested (nav) prop"
// https://github.com/aspnet/EntityFramework/issues/3676 "Usage of the "let" keyword breaks grouping" 


namespace Samples.Controllers {

    public class NorthwindController {
        NorthwindContext _nwind;

        public NorthwindController(NorthwindContext nwind) {
            _nwind = nwind;

#if DEBUG
            // Database connection string can be changed in Models\Northwind\NorthwindContext.cs
            // 'NORTHWND' database can be downloaded from https://northwinddatabase.codeplex.com/
            _nwind.Database.OpenConnection();
#endif
        }

        public object SalesCube() {
            return from d in _nwind.Order_Details
                   let p = d.Product
                   let o = d.Order
                   select new {
                       o.OrderDate,
                       p.ProductName,
                       p.Category.CategoryName,
                       Sum = d.Quantity * d.UnitPrice
                   };
        }

        public IActionResult Orders(DataSourceLoadOptions options) {
            return DataSourceLoadResult.Create(_nwind.Orders, options);
        }

        public IActionResult OrderDetails(int orderID, DataSourceLoadOptions options) {
            return DataSourceLoadResult.Create(
                from i in _nwind.Order_Details
                where i.OrderID == orderID
                select new {
                    i.Product.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    Sum = i.UnitPrice * i.Quantity
                },
                options
            );
        }

        public IActionResult CustomersLookup(DataSourceLoadOptions options) {
            return DataSourceLoadResult.Create(
                from c in _nwind.Customers
                orderby c.CompanyName
                select new { Value = c.CustomerID, Text = $"{c.CompanyName} ({c.Country})" },
                options
            );
        }

        public IActionResult ShippersLookup(DataSourceLoadOptions options) {
            return DataSourceLoadResult.Create(
                from s in _nwind.Shippers
                orderby s.CompanyName
                select new { Value = s.ShipperID, Text = s.CompanyName },
                options
            );
        }

        public void UpdateOrder(int key, string values) {
            var order = _nwind.Orders.First(o => o.OrderID == key);
            JsonConvert.PopulateObject(values, order);
            _nwind.SaveChanges();
        }

        public void InsertOrder(string values) {
            var order = new Order();
            JsonConvert.PopulateObject(values, order);
            _nwind.Orders.Add(order);
            _nwind.SaveChanges();
        }

        public void DeleteOrder(int key) {
            var order = _nwind.Orders.First(o => o.OrderID == key);
            _nwind.Orders.Remove(order);
            _nwind.SaveChanges();
        }

        public object ShipsByMonth(string shipper) {
            // NOTE see the #warning at the top of the file
            var temp = _nwind.Orders.Include(o => o.Shipper).ToArray();

            return from o in temp
                   where o.Shipper != null
                   orderby o.OrderDate
                   group o by o.OrderDate.Value.ToString("yyyy/MM") into g
                   select new {
                       Month = g.Key,
                       Amount = g.Count(o => o.Shipper.CompanyName == shipper),
                       TotalAmount = g.Count()
                   };
        }

        public object SalesByCategory() {
            // NOTE see the #warning at the top of the file
            var temp = _nwind.Order_Details.Include(d => d.Product.Category).ToArray();

            return from d in temp
                   group d by d.Product.Category.CategoryName into g
                   let sales = g.Sum(d => d.Quantity * d.UnitPrice)
                   orderby sales descending
                   select new {
                       Category = g.Key,
                       Sales = sales,
                       Count = g.Count()
                   };
        }

        public object SalesByCategoryYear() {
            // NOTE see the #warning at the top of the file
            var temp = _nwind.Order_Details.Include(d => d.Product.Category).Include(d => d.Order).ToArray();

            return from d in temp
                   let year = d.Order.OrderDate.Value.Year
                   let category = d.Product.Category.CategoryName
                   orderby year, category
                   group d by new { Year = year, Category = category } into g
                   select new {
                       g.Key.Year,
                       g.Key.Category,
                       Sales = g.Sum(d => d.Quantity * d.UnitPrice)
                   };
        }
    }

}