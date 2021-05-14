using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using YiSha.IdGenerator;
using YiSha.Util.Helper;
using YiSha.Web.Code;

namespace YiSha.Entity
{
    /// <summary>
    /// 数据库实体的基类，所有的数据库实体属性类型都是可空值类型，为了在做条件查询的时候进行判断
    /// 虽然是可空值类型，null的属性值，在底层会根据属性类型赋值默认值，字符串是string.empty，数值是0，日期是1970-01-01
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// 所有表的主键
        /// long返回到前端js的时候，会丢失精度，所以转成字符串
        /// </summary>
        [JsonConverter(typeof(StringJsonConverter))]
        public long? Id { get; set; }

        /// <summary>
        /// WebApi没有Cookie和Session，所以需要传入Token来标识用户身份
        /// </summary>
        [NotMapped]
        protected string Token { get; set; }

        protected virtual void Create()
        {
            Id = IdGeneratorHelper.Instance.GetId();
        }
    }

    public class BaseCreateEntity : BaseEntity
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonConverter(typeof(DateTimeJsonConverter)), Description("创建时间")]
        public DateTime? BaseCreateTime { get; set; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        public long? BaseCreatorId { get; set; }

        public new async Task Create()
        {
            base.Create();

            BaseCreateTime ??= DateTime.Now;

            if (BaseCreatorId == null)
            {
                var user = await Operator.Instance.Current(Token);
                BaseCreatorId = user?.UserId ?? 0;
            }
        }
    }

    public class BaseModifyEntity : BaseCreateEntity
    {
        /// <summary>
        /// 数据更新版本，控制并发
        /// </summary>
        public int? BaseVersion { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonConverter(typeof(DateTimeJsonConverter)), Description("修改时间")]
        public DateTime? BaseModifyTime { get; set; }

        /// <summary>
        /// 修改人ID
        /// </summary>
        public long? BaseModifierId { get; set; }

        protected async Task Modify()
        {
            BaseVersion = 0;
            BaseModifyTime = DateTime.Now;

            if (BaseModifierId == null)
            {
                var user = await Operator.Instance.Current();
                BaseModifierId = user?.UserId ?? 0;
            }
        }
    }

    public class BaseExtensionEntity : BaseModifyEntity
    {
        /// <summary>
        /// 是否删除 1是，0否
        /// </summary>
        [JsonIgnore]
        public int? BaseIsDelete { get; set; }

        public new async Task Create()
        {
            BaseIsDelete = 0;

            await base.Create();

            await base.Modify();
        }

        public new async Task Modify()
        {
            await base.Modify();
        }
    }

    public static class BaseField
    {
        public static readonly string[] BaseFieldList =
        {
            "Id",
            "BaseIsDelete",
            "BaseCreateTime",
            "BaseModifyTime",
            "BaseCreatorId",
            "BaseModifierId",
            "BaseVersion"
        };
    }
}