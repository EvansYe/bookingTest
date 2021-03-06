﻿/*----------------------------------------------------------------
* desc：yeheping.old_t_mt_device  的基本增删改查操作
* date：2019-08-30 14:50:34 
*----------------------------------------------------------------*/
using System;

namespace BookingPlatform.Core.TableModels
{
    ///<summary>
	///
	///</summary>
	public partial class old_t_mt_device
    {
        ///<summary>
        ///
        ///</summary>
        public string ID { get; set; }

        ///<summary>
        ///医院组织机构代码
        ///</summary>
        public string HospitalID { get; set; }

        ///<summary>
        ///设备编码
        ///</summary>
        public string DeviceCode { get; set; }

        ///<summary>
        ///设备名称
        ///</summary>
        public string DeviceName { get; set; }

        ///<summary>
        ///科室ID
        ///</summary>
        public string ClinicID { get; set; }

        ///<summary>
        ///群组ID
        ///</summary>
        public string DeviceGroupID { get; set; }

        ///<summary>
        ///设备类型ID
        ///</summary>
        public string DeviceTypeID { get; set; }

        ///<summary>
        ///放置位置
        ///</summary>
        public string PlacePosition { get; set; }

        ///<summary>
        ///购置时间
        ///</summary>
        public string AcquisitionDT { get; set; }

        ///<summary>
        ///设备使用情况
        ///</summary>
        public int? DeviceUsing { get; set; }

        ///<summary>
        ///备注
        ///</summary>
        public string Remark { get; set; }

        ///<summary>
        ///
        ///</summary>
        public int? IsDelete { get; set; }

        ///<summary>
        ///
        ///</summary>
        public DateTime? CreateDT { get; set; }

        ///<summary>
        ///关闭时间
        ///</summary>
        public string CloseDT { get; set; }
    }
}