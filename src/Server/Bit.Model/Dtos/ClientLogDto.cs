﻿using System;
using System.ComponentModel.DataAnnotations;
using Bit.Model.Contracts;

namespace Bit.Model.Dtos
{
    [Serializable]
    public class ClientLogDto : IDto
    {
        [Key]
        [Required]
        public virtual string Message { get; set; }

        public virtual string Route { get; set; }

        public virtual DateTimeOffset ClientDate { get; set; }

        public virtual string Error { get; set; }

        public virtual string ErrorName { get; set; }

        public virtual string AdditionalInfo { get; set; }

        public virtual string StackTrace { get; set; }

        public virtual bool ClientWasOnlie { get; set; }

        public virtual string LogLevel { get; set; }
    }
}
