using NUnit.Framework;
using YiSha.Util.Model;

namespace YiSha.DataTest
{
    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GlobalContext.SystemConfig = new SystemConfig
            {
                DbProvider = "MySql",
                DbConnectionString = "server=121.40.169.153;database=yishaadmin;user=root;password=fy?xK/qYR75e;port=3306;",
                DbCommandTimeout = 180,
                DbBackup = "DataBase"
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }
    }
}