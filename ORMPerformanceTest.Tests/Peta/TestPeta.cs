﻿using System;
using System.ComponentModel.Composition;
using System.Linq;
using ORMPerformanceTest.TestData;
using ORMPerformanceTest.Tests.Bulk;
using ORMPerformanceTest.Tests.Helpers;

namespace ORMPerformanceTest.Tests.Peta
{
    [Export(typeof(ITest))]
    public class TestPeta : ITest
    {
        public TestPeta()
        {
            TestKind = "Peta Poco";
        }
        public string TestKind { get; private set; }
        public TestResult Count(string connectionString)
        {
            return TestRunner.RunTest(CountTest, connectionString, string.Format("{0} {1}", TestKind, "Count"));
        }
        public TestResult Insert(string connectionString)
        {
            return TestRunner.RunTest(InsertTest, connectionString, string.Format("{0} {1}", TestKind, "Insert"));
        }
        public TestResult SelectAll(string connectionString)
        {
            return TestRunner.RunTest(SelectAllTest, connectionString, string.Format("{0} {1}", TestKind, "SelectAll"));
        }

        public TestResult SelectPart(string connectionString)
        {
            return TestRunner.RunTest(SelectPartTest, connectionString, string.Format("{0} {1}", TestKind, "SelectPart"));
        }

        public TestResult SelectJoin(string connectionString)
        {
            return TestRunner.RunTest(SelectJoinTest, connectionString, string.Format("{0} {1}", TestKind, "SelectJoin"));
        }

        public TestResult Update100(string connectionString)
        {
            return TestRunner.RunTest(Update100Test, connectionString, string.Format("{0} {1}", TestKind, "Update100"));
        }

        public TestResult Delete100(string connectionString)
        {
            return TestRunner.RunTest(Delete100Test, connectionString, string.Format("{0} {1}", TestKind, "Delete100"));
        }
        public void InitDataBase(string connectionString)
        {
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            db.Execute(Const.DBCreateScript);
            foreach (var province in ProvinceData.GetProvinces())
            {
                db.Insert("Province", "Id", new{Name = province.Name, Code = province.Code});
            }
            var provinces = db.Query<dynamic>(@"SELECT *  
from Province").ToList();
            BulkUploadToSql bulk =
                   BulkUploadToSql.Load(
                       HomeData.GetHomes()
                           .Select(
                               i =>
                                   new Bulk.Home
                                   {
                                       AddTime = DateTime.Now,
                                       BuildYear = i.BuildYear,
                                       City = i.City,
                                       Description = i.Description,
                                       Price = i.Price,
                                       Surface = i.Surface,
                                       ProvinceId = provinces.First(j => j.Code == i.HomeProvince.Code).Id,
                                   }), "Home", 10000, connectionString);
            bulk.Flush();

        }

        public int Priority { get { return 2; } }

        #region [Private]
        private static int CountTest(string connectionString)
        {
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            long count = db.ExecuteScalar<long>("SELECT Count(*) FROM Home");
            return 1;
        }

        private static int SelectAllTest(string connectionString)
        {
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            return db.Query<Home>("SELECT * FROM Home").ToList().Count();
        }

        private static int SelectPartTest(string connectionString)
        {
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            return db.Query<Home>("SELECT * FROM Home where BuildYear<@0", 2000).ToList().Count();
        }

        private static int SelectJoinTest(string connectionString)
        {
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            return db.Query<dynamic>(@"SELECT h.*  
from Home h
inner join Province p on h.ProvinceId=p.Id
where p.Code=@0", 10).ToList().Count();
        }

        private static int InsertTest(string connectionString)
        {
            Home pocoHome;
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            var provinces = db.Query<dynamic>(@"SELECT *  
from Province");
            foreach (var home in HomeData.Get100Homes())
            {
                pocoHome = new Home
                {
                    BuildYear = home.BuildYear,
                    City = home.City,
                    Description = home.Description,
                    ProvinceId = provinces.First(i => i.Code == home.HomeProvince.Code).Id,
                    Price = home.Price,
                    Surface = home.Surface,
                    AddTime = DateTime.Now
                };
                db.Insert("Home", "Id", pocoHome);
            }
            return 100;
        }

        private static int Update100Test(string connectionString)
        {
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            var homes = db.Query<Home>("SELECT * FROM home WHERE BuildYear=@0", 2014).ToList();
            int count = homes.Count();
            foreach (var home in homes)
            {
                if (home != null)
                {
                    home.BuildYear = 2015;
                    db.Update("Home", "Id", home);
                }
            }
            return count;
        }

        private static int Delete100Test(string connectionString)
        {
            var db = new PetaPoco.Database(connectionString, "System.Data.SqlClient");
            var homes = db.Query<Home>("SELECT * FROM home WHERE BuildYear=@0", 2015).ToList();
            int count = homes.Count();
            foreach (var home in homes)
            {
                if (home != null)
                {
                    home.BuildYear = 2015;
                    db.Delete("Home", "Id", home);
                }
            }
            return count;
        }
        #endregion [Private]
    }
}