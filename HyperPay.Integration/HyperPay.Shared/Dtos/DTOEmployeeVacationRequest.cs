﻿using System;
using System.ComponentModel.DataAnnotations;

namespace HyperPay.Shared.Dtos
{
    public class DTOEmployeeVacationRequest
    {
        public int EmployeeID { get; set; }

        [Required(ErrorMessage = "From Date is required.")]

        [RegularExpression(@"^(((((0[1-9])|(1\d)|(2[0-8]))-((0[1-9])|(1[0-2])))|((31-((0[13578])|(1[02])))|((29|30)-((0[1,3-9])|(1[0-2])))))-((20[0-9][0-9]))|(29-02-20(([02468][048])|([13579][26]))))$", ErrorMessage = "Invalid from date format it's should be (dd-mm-yyyy).")]
        public string FromDate { get; set; }


        [Required(ErrorMessage = "To Date is required.")]
        [RegularExpression(@"^(((((0[1-9])|(1\d)|(2[0-8]))-((0[1-9])|(1[0-2])))|((31-((0[13578])|(1[02])))|((29|30)-((0[1,3-9])|(1[0-2])))))-((20[0-9][0-9]))|(29-02-20(([02468][048])|([13579][26]))))$", ErrorMessage = "Invalid to date format it's should be (dd-mm-yyyy).")]
        public string ToDate { get; set; }
    }

    public class DTOEmployeeVacationResponse
    {
        public string EMP_NUMBER { get; set; }
        public string EMP_NAME { get; set; }
        public string ENG_NAME { get; set; }
        public string POSITION { get; set; }
        public string VACATION_ID { get; set; }
        public string VACATION_NAME { get; set; }
        public string START_DATE { get; set; }
        public string END_DATE { get; set; }
        public string ABSENCE_DAYS { get; set; }
        public string EMP_CATID { get; set; }
        public string EMP_CATEGORY { get; set; }
    }
}
