using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace HyperPay.Mobile.Helpers
{
    public enum IDTypes
    {
        NA,
        IqamaID,
        NationalID,
        GCC,
        Passport
    }

    public static class ExtentionsService
    {
        /// <summary>
        /// Remove HTML from string with compiled Regex.

        /// </summary>
        public static string RemoveHtmlTags(this string source)
        {
            if (string.IsNullOrEmpty(source)) return "";
            var s = source.Trim();
            char[] array = new char[s.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < s.Length; i++)
            {
                char let = s[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
        public static string ConvertToString(this byte[] bytes)
        {
            if (bytes == null || bytes.Length <= 1)
            {
                return "";
            }

            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
        public static byte[] ConvertToBytes(this string str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string SerializeObjectToXml<T>(this T toSerialize)
        {
            if (toSerialize == null) return null;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }


        public static string SerializeToXml<T>(this T value)
        {
            if (value == null) return null;


            XmlSerializer serializer = new XmlSerializer(typeof(T));

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UnicodeEncoding(false, false),
                Indent = false,
                OmitXmlDeclaration = false
            };
            // no BOM in a .NET string

            using (var textWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value);
                }
                return textWriter.ToString();
            }
        }


        //public static string SerializeJson<T>(this object jsonResult)
        //{
        //    return JsonConvert.SerializeObject(jsonResult);
        //}

        // Convert Array to String
        public static string UTF8ByteArrayToString(byte[] ArrBytes)
        {
            return ArrBytes == null ? null : new UTF8Encoding().GetString(ArrBytes);
        }
        // Convert String to Array
        public static byte[] StringToUTF8ByteArray(string XmlString)
        { return string.IsNullOrEmpty(XmlString) ? null : new UTF8Encoding().GetBytes(XmlString); }

        public static T DeserializeXml<T>(this string xml)
        {
            //T tempObject = default(T);

            //using (MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(xml)))
            //{
            //    XmlSerializer xs = new XmlSerializer(typeof(T));
            //    XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

            //    tempObject = (T)xs.Deserialize(memoryStream);
            //}

            //return tempObject;


            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            var serializer = new XmlSerializer(typeof(T));

            var settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true
            };

            // No settings need modifying here

            using (var textReader = new StringReader(xml))
            {
                using (var xmlReader = XmlReader.Create(textReader, settings))
                {
                    xmlReader.ReadToDescendant("Body");
                    return (T)serializer.Deserialize(xmlReader.ReadSubtree());
                }
            }
        }

        //public static T DeserializeJson<T>(this string jsonResult)
        //{
        //    return null; //JsonConvert.DeserializeObject<T>(jsonResult);
        //}

        public static T DeserializeXml<T>(this XmlReader xml)
        {
            try
            {

                if (xml == null)
                {
                    return default(T);
                }

                var serializer = new XmlSerializer(typeof(T));

                var settings = new XmlReaderSettings();
                // No settings need modifying here




                return (T)serializer.Deserialize(xml);



            }
            catch (Exception ex)
            {
                return default(T);
            }
            //using (var textReader = new StringReader(xml))
            //{
            //    using (var xmlReader = XmlReader.Create(textReader, settings))
            //    {
            //        return (T)serializer.Deserialize(xmlReader);
            //    }
            //}
        }





        //public static HashSet<T> ToHashSetOld<T>(this IEnumerable<T> source)
        //{
        //    if (source != null)
        //        return new HashSet<T>(source);
        //    else
        //        return null;
        //}
        public static HashSet<TElement> ToHashSet<T, TElement>(this IEnumerable<T> source,
          Func<T, TElement> elementSelector, IEqualityComparer<TElement> comparer)
        {
            if (source == null) return default(HashSet<TElement>);// throw new ArgumentNullException("source");
            if (elementSelector == null) throw new ArgumentNullException("elementSelector");

            // you can unroll this into a foreach if you want efficiency gain, but for brevity...
            return new HashSet<TElement>(source.Select(elementSelector), comparer);
        }
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            // key selector is identity fxn and null is default comparer
            return source.ToHashSet<T, T>(item => item, null);
        }
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source,
           IEqualityComparer<T> comparer)
        {
            return source.ToHashSet<T, T>(item => item, comparer);
        }

        public static HashSet<TElement> ToHashSet<T, TElement>(this IEnumerable<T> source,
           Func<T, TElement> elementSelector)
        {
            return source.ToHashSet<T, TElement>(elementSelector, null);
        }

        public static string RemoveAllSpaces(this string str)
        {
            string cleanString = Regex.Replace(str, @"\s+", "");
            return cleanString;

        }
        public static Stream ToStream(this string @this)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(@this);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        //  private static JavaScriptSerializer json;
        // private static JavaScriptSerializer JSON => json ?? (json = new JavaScriptSerializer());

        public static T ParseXML<T>(this string @this) where T : class
        {
            var reader = XmlReader.Create(@this.Trim().ToStream(), new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Document });
            return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
        }
        //  public static T ParseJSON<T>(this string @this) where T : class
        // {
        //     return json.Deserialize<T>(@this.Trim());
        //  }

        public static T Cast<T>(this object obj) where T : class
        {
            return obj as T;
        }

        public static string Trim(this string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return val.Trim();
        }
        public static string ConvertToRequestNumber(this string reqNumberd, int num)
        {
            var reqNumber = "";
            if (num < 10) reqNumber = "0000" + num;
            else if (num >= 10 && num < 100) reqNumber = "000" + num;
            else if (num >= 100 && num < 1000) reqNumber = "00" + num;
            else if (num >= 10000 && num < 100000) reqNumber = "0" + num;
            else if (num >= 100000) reqNumber = num.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return reqNumber;
        }
        public static int ToInt(this string text)
        {
            int x = 0;
            int.TryParse(text, out x);
            return x;


        }

        public static int ToInt(this string input, int defaultValue)
        {
            var x = 0;
            return int.TryParse(input, out x) ? x : defaultValue;
        }


        public static decimal? ToDecimal(this string input)
        {
            decimal x = 0;
            if (decimal.TryParse(input, out x))
                return x;
            return null;

        }
        public static decimal ToDecimal(this string input, int defaultValue)
        {

            return decimal.TryParse(input, out var x) ? x : defaultValue;


        }
        public static decimal? ToDecimal(this double? input)
        {

            return Convert.ToDecimal(input);


        }

        //public struct JsonResult
        //{
        //    public JsonResultData Data { get; set; }
        //}

        //public struct JsonResultData
        //{
        //    public string Tag { get; set; }
        //    public JsonResultState State { get; set; }
        //}
        //public struct JsonResultState
        //{
        //    public string Name { get; set; }
        //    public List<string> Errors { get; set; }
        //}
        //public static JsonResult JsonValidation(this ModelStateDictionary state)
        //{
        //    return new JsonResult
        //    {
        //        Data = new JsonResultData()
        //        {
        //            Tag = "ValidationError",
        //            State = from e in state
        //                    where e.Value.Errors.Count > 0
        //                    select new
        //                    {
        //                        Name = e.Key,
        //                        Errors = e.Value.Errors.Select(x => x.ErrorMessage)
        //                          .Concat(e.Value.Errors.Where(x => x.Exception != null).Select(x => x.Exception.Message))
        //                    }
        //        }
        //    };
        //}


        public static string SerializeJson<T>(this T value)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);

                return json;


            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static T DeserializeJson<T>(this string value) where T : class
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);

                return json;


            }
            catch (Exception ex)
            {
                return null;
                // return default(T);
            }
        }





        public static string ToArabicNumber(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            try
            {

                var utf8Encoder = new UTF8Encoding();
                var utf8Decoder = utf8Encoder.GetDecoder();
                var convertedChars = new System.Text.StringBuilder();
                var convertedChar = new char[1];
                var bytes = new byte[] { 217, 160 };
                var inputCharArray = input.ToCharArray();

                foreach (var c in inputCharArray)
                {
                    if (char.IsDigit(c))
                    {
                        bytes[1] = Convert.ToByte(160 + char.GetNumericValue(c));
                        utf8Decoder.GetChars(bytes, 0, 2, convertedChar, 0);
                        convertedChars.Append(convertedChar[0]);
                    }
                    else
                    {
                        convertedChars.Append(c);
                    }
                }
                return convertedChars.ToString();
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public static string ToEnglishNumber(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            try
            {
                var englishNumbers = "";


                for (var i = 0; i < input.Length; i++)
                {
                    if (char.IsDigit(input[i]))
                    {
                        englishNumbers += char.GetNumericValue(input, i);

                    }
                    else
                    {
                        englishNumbers += input[i].ToString();
                    }


                }
                return englishNumbers.Trim();


            }
            catch (Exception e)
            {
                return "";
            }

        }


        /// <summary>
        /// Not Completed yet.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string input, string format = "MM/dd/YYYY")
        {
            var dt = new DateTime();
            return DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ? dt : DateTime.MinValue;
        }

        public static double? ToDouble(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            double result = 0.0;
            if (double.TryParse(input, out result))
            {
                return result;
            }
            return null;


        }
        public static double? ToFloat(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            float result = 0;
            if (float.TryParse(input, out result))
            {
                return result;
            }
            return null;


        }

        public static bool ToBool(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }


            value = value.Trim();
            value = value.ToLower();

            switch (value)
            {
                case "true":
                case "t":
                case "1":
                case "yes":
                case "y":
                    return true;
                default:
                    return false;
            }

        }

        public static T ToEnum<T>(this string value) where T : IComparable, IFormattable
        {

            var res = (T)Enum.Parse(typeof(T), value, false);
            return res;

        }


        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> act)
        {
            foreach (T element in source) act(element);
            return source;
        }


        public static bool IsEmailValid(this string input)
        {
            bool isEmail = System.Text.RegularExpressions.Regex.IsMatch(input.Trim(),
             @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
             System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return isEmail;
        }



        public static string CreatePrefixPayment(this string input, int systemId)
        {
            var reqNumber = "";
            if (string.IsNullOrEmpty(input)) return "";


            switch (systemId)
            {

                case 1086:
                    switch (input.Length)
                    {
                        case 1:
                            reqNumber = "11000000000" + input;
                            break;
                        case 2:
                            reqNumber = "1100000000" + input;
                            break;
                        case 3:
                            reqNumber = "110000000" + input;
                            break;
                        case 4:
                            reqNumber = "11000000" + input;
                            break;
                        case 5:
                            reqNumber = "1100000" + input;
                            break;
                        case 6:
                            reqNumber = "110000" + input;
                            break;
                        case 7:
                            reqNumber = "11000" + input;
                            break;
                        case 8:
                            reqNumber = "1100" + input;
                            break;
                        case 9:
                            reqNumber = "110" + input;
                            break;
                        case 10:
                            reqNumber = "11" + input;
                            break;
                        default:
                            reqNumber = "1" + input;
                            break;
                    }

                    break;
                case 1087:
                    switch (input.Length)
                    {
                        case 1:
                            reqNumber = "21000000000" + input;
                            break;
                        case 2:
                            reqNumber = "2100000000" + input;
                            break;
                        case 3:
                            reqNumber = "210000000" + input;
                            break;
                        case 4:
                            reqNumber = "21000000" + input;
                            break;
                        case 5:
                            reqNumber = "2100000" + input;
                            break;
                        case 6:
                            reqNumber = "210000" + input;
                            break;
                        case 7:
                            reqNumber = "21000" + input;
                            break;
                        case 8:
                            reqNumber = "2100" + input;
                            break;
                        case 9:
                            reqNumber = "210" + input;
                            break;
                        case 10:
                            reqNumber = "21" + input;
                            break;
                        default:
                            reqNumber = "2" + input;
                            break;

                    }

                    break;
                case 1088:
                    switch (input.Length)
                    {
                        case 1:
                            reqNumber = "31000000000" + input;
                            break;
                        case 2:
                            reqNumber = "3100000000" + input;
                            break;
                        case 3:
                            reqNumber = "310000000" + input;
                            break;
                        case 4:
                            reqNumber = "31000000" + input;
                            break;
                        case 5:
                            reqNumber = "3100000" + input;
                            break;
                        case 6:
                            reqNumber = "310000" + input;
                            break;
                        case 7:
                            reqNumber = "31000" + input;
                            break;
                        case 8:
                            reqNumber = "3100" + input;
                            break;
                        case 9:
                            reqNumber = "310" + input;
                            break;
                        case 10:
                            reqNumber = "31" + input;
                            break;
                        default:
                            reqNumber = "3" + input;
                            break;
                    }

                    break;
                default:
                    reqNumber = "";
                    break;
            }

            return reqNumber;
        }

        public static bool IsIdValid(this string value, IDTypes? IDType)
        {
            switch (IDType)
            {
                case null:
                    return false;

                case IDTypes.NA:
                    return false;

                case IDTypes.NationalID:
                    {
                        Regex Rex = new Regex(@"^\d(\d{8,10})$");
                        //
                        if (Rex.IsMatch(value))
                        {
                            if (IsIdValid(value))
                            {

                            }
                            else
                            {
                                return false;
                            }

                        }
                        else
                        {
                            return false;
                        }
                    }
                    break;

                case IDTypes.IqamaID:
                    {
                        Regex Rex = new Regex(@"^[2](\d{8,8}\d)$");
                        //
                        if (Rex.IsMatch(value))
                        {
                            if (IsIdValid(value))
                            {

                            }
                            else
                            {
                                return false;
                            }

                        }
                        else
                        {
                            return false;
                        }
                    }
                    break;
                case IDTypes.GCC:
                    {
                        Regex Rex = new Regex(@"^\d(\d{8,10})$");

                        if (!Rex.IsMatch(value))
                        {
                            return false;
                        }


                    }
                    break;
                case IDTypes.Passport:
                    if (value.Length > 20)
                    {
                        return false;
                    }
                    break;
                default:
                    return false;


            }

            return false;
        }

        private static bool IsIdValid(string ID)
        {
            bool RetValue = false;
            try
            {
                string identity = ID;
                if (identity.Length < 9)//|| !IsNumber(ref identity))
                    return false;
                identity = identity.ToEnglishNumber();

                StringBuilder digits = new StringBuilder(15);
                for (int i = 0; i <= 8; i++)
                {
                    if ((i + 1) % 2 == 0)
                        digits.Append(identity.Substring(i, 1));
                    else
                        digits.Append((int.Parse(identity.Substring(i, 1)) * 2).ToString());
                }

                int digitsSum = 0;
                for (int i = 0; i < digits.Length; i++)
                    digitsSum += int.Parse(digits[i].ToString());

                string sum = digitsSum.ToString();
                short oddSumDigit = (digitsSum.ToString().Length == 1 ? Convert.ToInt16(sum) : Convert.ToInt16(sum.Substring(sum.Length - 1, 1)));

                string result;
                if (oddSumDigit == 0)
                    result = "0";
                else
                    result = (10 - oddSumDigit).ToString();

                RetValue = (result == identity.Substring(identity.Length - 1, 1));
            }
            catch
            {
                RetValue = false;
            }
            return RetValue;


        }

    }
}