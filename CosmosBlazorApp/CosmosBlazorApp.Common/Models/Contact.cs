using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosBlazorApp.Common.Models
{
    public class Contact
    {
        [JsonProperty("id")]
        public string ContactId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public Gender Gender { get; set; }
        public string ContactType { get; set; }
    }
}
