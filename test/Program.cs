using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HL7.Dotnetcore.Test
{
    [TestClass]
    public class HL7Test
    {
        private string HL7_ORM;
        private string HL7_ADT;

        public static void Main(string[] args)
        {
            var test = new HL7Test();
            // test.CustomDelimiter();
            test.AddRepeatingField();
        }

        public HL7Test()
        {
            var path = Path.GetDirectoryName(typeof(HL7Test).GetTypeInfo().Assembly.Location) + "/";
            this.HL7_ORM = File.ReadAllText(path + "Sample-ORM.txt");
            this.HL7_ADT = File.ReadAllText(path + "Sample-ADT.txt");
        }

        [TestMethod]
        public void SmokeTest()
        {
            Message message = new Message(this.HL7_ORM);
            Assert.IsNotNull(message);

            // message.ParseMessage();
            // File.WriteAllText("SmokeTestResult.txt", message.SerializeMessage(false));
        }

        [TestMethod]
        public void ParseTest1()
        {
            var message = new Message(this.HL7_ORM);

            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
        }

        [TestMethod]
        public void ParseTest2()
        {
            var message = new Message(this.HL7_ADT);

            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
        }


        [TestMethod]
        public void ReadSegmentTest()
        {
            var message = new Message(this.HL7_ORM);
            message.ParseMessage();

            Segment MSH_1 = message.Segments("MSH")[0];
            Assert.IsNotNull(MSH_1);
        }

        [TestMethod]
        public void ReadDefaultSegmentTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            Segment MSH = message.DefaultSegment("MSH");
            Assert.IsNotNull(MSH);
        }

        [TestMethod]
        public void ReadFieldTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_9 = message.GetValue("MSH.9");
            Assert.AreEqual("ADT^O01", MSH_9);
        }

        [TestMethod]
        public void ReadFieldTest2()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var NK1_2_2 = message.GetValue("NK1(2).2");
            Assert.AreEqual("DOE^JHON^^^^", NK1_2_2);
        }

        [TestMethod]
        public void ReadComponentTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_9_1 = message.GetValue("MSH.9.1");
            Assert.AreEqual("ADT", MSH_9_1);
        }

        [TestMethod]
        public void AddComponentsTest()
        {
            var encoding = new HL7Encoding();

            // Create a Segment with name ZIB
            Segment newSeg = new Segment("ZIB", encoding);

            // Create Field ZIB_1
            Field ZIB_1 = new Field("ZIB1", encoding);
            // Create Field ZIB_5
            Field ZIB_5 = new Field("ZIB5", encoding);

            // Create Component ZIB.5.3
            Component com1 = new Component("ZIB.5.3_", encoding);

            // Add Component ZIB.5.3 to Field ZIB_5
            ZIB_5.AddNewComponent(com1, 3);

            // Overwrite the same field again
            ZIB_5.AddNewComponent(new Component("ZIB.5.3", encoding), 3);

            // Add Field ZIB_1 to segment ZIB, this will add a new filed to next field location, in this case first field
            newSeg.AddNewField(ZIB_1);

            // Add Field ZIB_5 to segment ZIB, this will add a new filed as 5th field of segment
            newSeg.AddNewField(ZIB_5, 5);

            // Add segment ZIB to message
            var message = new Message(this.HL7_ADT);
            message.AddNewSegment(newSeg);

            string serializedMessage = message.SerializeMessage(false);
            Assert.AreEqual("ZIB|ZIB1||||ZIB5^^ZIB.5.3\r", serializedMessage);
        }

        [TestMethod]
        public void EmptyFieldsTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var NK1 = message.DefaultSegment("NK1").GetAllFields();
            Assert.AreEqual(34, NK1.Count);
            Assert.AreEqual(string.Empty, NK1[33].Value);
        }

        [TestMethod]
        public void NotEncodingTest()
        {
            var enc = new HL7Encoding().Encode("<1");
            Assert.AreEqual(enc, "<1");
        }

        [TestMethod]
        public void EncodingForOutputTest()
        {
            const string oruUrl = "domain.com/resource.html?Action=1&ID=2";  // Text with special character (&)

            var obx = new Segment("OBX", new HL7Encoding());
            obx.AddNewField("1");
            obx.AddNewField("RP");
            obx.AddNewField("70030^Radiologic Exam, Eye, Detection, FB^CDIRadCodes");
            obx.AddNewField("1");
            obx.AddNewField(obx.Encoding.Encode(oruUrl));  // Encoded field
            obx.AddNewField("F", 11);
            obx.AddNewField(MessageHelper.LongDateWithFractionOfSecond(DateTime.Now), 14);

            var oru = new Message();
            oru.AddNewSegment(obx);

            var str = oru.SerializeMessage(false);

            Assert.IsFalse(str.Contains("&"));  // Should have \T\ instead
        }

        [TestMethod]
        public void AddFieldTest()
        {
            var enc = new HL7Encoding();
            Segment PID = new Segment("PID", enc);
            // Creates a new Field
            PID.AddNewField("1", 1);

            // Overwrites the old Field
            PID.AddNewField("2", 1);

            Message message = new Message();
            message.AddNewSegment(PID);
            var str = message.SerializeMessage(false);

            Assert.AreEqual("PID|2\r", str);
        }

        [TestMethod]
        public void GetMSH1Test()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_1 = message.GetValue("MSH.1");
            Assert.AreEqual("|", MSH_1);
        }

        [TestMethod]
        public void GetAckTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();
            var ack = message.GetACK();

            var MSH_3 = message.GetValue("MSH.3");
            var MSH_4 = message.GetValue("MSH.4");
            var MSH_5 = message.GetValue("MSH.5");
            var MSH_6 = message.GetValue("MSH.6");
            var MSH_3_A = ack.GetValue("MSH.3");
            var MSH_4_A = ack.GetValue("MSH.4");
            var MSH_5_A = ack.GetValue("MSH.5");
            var MSH_6_A = ack.GetValue("MSH.6");

            Assert.AreEqual(MSH_3, MSH_5_A);
            Assert.AreEqual(MSH_4, MSH_6_A);
            Assert.AreEqual(MSH_5, MSH_3_A);
            Assert.AreEqual(MSH_6, MSH_4_A);

            var MSH_10 = message.GetValue("MSH.10");
            var MSH_10_A = ack.GetValue("MSH.10");
            var MSA_1_1 = ack.GetValue("MSA.1");
            var MSA_1_2 = ack.GetValue("MSA.2");

            Assert.AreEqual(MSA_1_1, "AA");
            Assert.AreEqual(MSH_10, MSH_10_A);
            Assert.AreEqual(MSH_10, MSA_1_2);
        }

        [TestMethod]
        public void AddSegmentMSHTest()
        {
            var message = new Message();
            message.AddSegmentMSH("test", "sendingFacility", "test", "test", "test", "ADR^A19", "test", "D", "2.5");
        }

        [TestMethod]
        public void GetNackTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var error = "Error message";
            var code = "AR";
            var ack = message.GetNACK(code, error);

            var MSH_3 = message.GetValue("MSH.3");
            var MSH_4 = message.GetValue("MSH.4");
            var MSH_5 = message.GetValue("MSH.5");
            var MSH_6 = message.GetValue("MSH.6");
            var MSH_3_A = ack.GetValue("MSH.3");
            var MSH_4_A = ack.GetValue("MSH.4");
            var MSH_5_A = ack.GetValue("MSH.5");
            var MSH_6_A = ack.GetValue("MSH.6");

            Assert.AreEqual(MSH_3, MSH_5_A);
            Assert.AreEqual(MSH_4, MSH_6_A);
            Assert.AreEqual(MSH_5, MSH_3_A);
            Assert.AreEqual(MSH_6, MSH_4_A);

            var MSH_10 = message.GetValue("MSH.10");
            var MSH_10_A = ack.GetValue("MSH.10");
            var MSA_1_1 = ack.GetValue("MSA.1");
            var MSA_1_2 = ack.GetValue("MSA.2");
            var MSA_1_3 = ack.GetValue("MSA.3");

            Assert.AreEqual(MSH_10, MSH_10_A);
            Assert.AreEqual(MSH_10, MSA_1_2);
            Assert.AreEqual(MSA_1_1, code);
            Assert.AreEqual(MSA_1_3, error);
        }

        [TestMethod]
        public void EmptyAndNullFieldsTest()
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617||\"\"\r\n";

            var message = new Message(sampleMessage);
            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
            Assert.IsTrue(message.SegmentCount > 0);
            var evn = message.Segments("EVN")[0];
            var expectEmpty = evn.Fields(3).Value;
            Assert.AreEqual(string.Empty, expectEmpty);
            var expectNull = evn.Fields(4).Value;
            Assert.AreEqual(null, expectNull);
        }

        [TestMethod]
        public void MessageWithDoubleNewlineTest()
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\n\nEVN|A04|20110613083617||\r\n";

            var message = new Message(sampleMessage);
            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
            Assert.IsTrue(message.SegmentCount > 0);
        }

        [TestMethod]
        public void MessageWithDoubleCarriageReturnTest()
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\n\nEVN|A04|20110613083617||\r\n";

            var message = new Message(sampleMessage);
            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
            Assert.IsTrue(message.SegmentCount > 0);
        }

        [TestMethod]
        public void MessageWithNullsIsReversable()
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617||\"\"\r\n";
            var message = new Message(sampleMessage);
            message.ParseMessage();
            var serialized = message.SerializeMessage(false);
            Assert.AreEqual(sampleMessage, serialized);
        }

        [TestMethod]
        public void MessageWithSegmentNameOnly()
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nPID\r\nEVN|A04|20110613083617||\"\"\r\n";
            var message = new Message(sampleMessage);
            message.ParseMessage();
            var serialized = message.SerializeMessage(false);
            Assert.AreEqual(sampleMessage, serialized);
        }

        [TestMethod]
        public void MessageWithTabsIsReversable()
        {
            const string sampleMessage = "MSH|^~\\&|Sending\tApplication|Sending\tFacility|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617\r\n";
            var message = new Message(sampleMessage);
            message.ParseMessage();
            var serialized = message.SerializeMessage(false);
            Assert.AreEqual(sampleMessage, serialized);
        }

        [TestMethod]
        public void RemoveSegment()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();
            Assert.AreEqual(message.Segments("NK1").Count, 2);
            Assert.AreEqual(message.SegmentCount, 5);

            message.RemoveSegment("NK1", 1);
            Assert.AreEqual(message.Segments("NK1").Count, 1);
            Assert.AreEqual(message.SegmentCount, 4);

            message.RemoveSegment("NK1");
            Assert.AreEqual(message.Segments("NK1").Count, 0);
            Assert.AreEqual(message.SegmentCount, 3);
        }

        [DataTestMethod]
        [DataRow("   20151231234500.1234+2358   ")]
        [DataRow("20151231234500.1234+2358")]
        [DataRow("20151231234500.1234-2358")]
        [DataRow("20151231234500.1234")]
        [DataRow("20151231234500.12")]
        [DataRow("20151231234500")]
        [DataRow("201512312345")]
        [DataRow("2015123123")]
        [DataRow("20151231")]
        [DataRow("201512")]
        [DataRow("2015")]
        public void ParseDateTime_Smoke_Positive(string dateTimeString)
        {
            var date = MessageHelper.ParseDateTime(dateTimeString);
            Assert.IsNotNull(date);
        }

        [DataTestMethod]
        [DataRow("   20151231234500.1234+23581")]
        [DataRow("20151231234500.1234+23")]
        [DataRow("20151231234500.12345")]
        [DataRow("20151231234500.")]
        [DataRow("2015123123450")]
        [DataRow("20151231234")]
        [DataRow("201512312")]
        [DataRow("2015123")]
        [DataRow("20151")]
        [DataRow("201")]
        public void ParseDateTime_Smoke_Negative(string dateTimeString)
        {
            var date = MessageHelper.ParseDateTime(dateTimeString);
            Assert.IsNull(date);
        }

        [TestMethod]
        public void ParseDateTime_Correctness()
        {
            TimeSpan offset;
            var date = MessageHelper.ParseDateTime("20151231234500.1234-2358", out offset).Value;
            // Assert.AreEqual(0, d
            Assert.AreEqual(date, new DateTime(2015, 12, 31, 23, 45, 00, 123));
            Assert.AreEqual(offset, new TimeSpan(-23, 58, 0));
        }

        [TestMethod]
        public void ParseDateTime_WithException()
        {
            try
            {
                var date = MessageHelper.ParseDateTime("201", true);
                Assert.Fail();
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch
            {
            }
        }

        [TestMethod]
        public void ParseDateTimeOffset_WithException()
        {
            try
            {
                var date = MessageHelper.ParseDateTime("201", out TimeSpan offset, true);
                Assert.Fail();
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch
            {
            }
        }

        [TestMethod]
        public void GetValueTest()
        {
            var sampleMessage =
                @"MSH|^~\&|EPIC||||20191107134803|ALEVIB01|ORM^O01|23|T|2.3|||||||||||
PID|1||MRN_123^^^IDX^MRN||Smith\F\\S\\R\\E\\T\^John||19600101|M";

            var message = new Message(sampleMessage);
            message.ParseMessage();

            string attendingDrId = message.GetValue("PID.5.1");
            Assert.AreEqual(@"Smith|^~\&", attendingDrId);
        }

        [TestMethod]
        public void SkipInvalidEscapeSequenceTest()
        {
            var sampleMessage =
                @"MSH|^~\&|TEST^TEST|TEST|||11111||ADT^A08|11111|P|2.4|||AL||D||||||
ZZ1|1|139378|20201230100000|ghg^ghgh-HA||s1^OP-Saal 1|gh^gjhg 1|ghg^ghjg-HA|BSV 4\5 re.||||||";

            var message = new Message(sampleMessage);
            message.ParseMessage();

            string serializedMessage = message.SerializeMessage(false);
        }

        [TestMethod]
        public void CustomDelimiterTest()
        {
            var encoding = new HL7Encoding
            {
                FieldDelimiter = '1',
                ComponentDelimiter = '2',
                SubComponentDelimiter = '3',
                RepeatDelimiter = '4',
                EscapeCharacter = '5'
            };

            var message = new Message();
            message.Encoding = encoding;
            message.AddSegmentMSH("FIRST", "SECOND", "THIRD", "FOURTH",
                "FIFTH", "ORU2R05F5", "SIXTH", "SEVENTH", "2.8");
            var result = message.SerializeMessage(false);

            Assert.AreEqual("MSH124531", result.Substring(0, 9));
        }

        [DataTestMethod]
        [DataRow("PV1.7.1", "1447312459")]
        [DataRow("PV1.7(1).1", "1447312459")]
        [DataRow("PV1.7[1].1", "1447312459")]
        [DataRow("PV1.7(2).1", "DOEM06")]
        [DataRow("PV1.7[2].1", "DOEM06")]
        [DataRow("PV1.7[2].3", "MICHAEL")]
        public void RepetitionTest(string index, string expected)
        {
            var sampleMessage =
                @"MSH|^~\&|EPIC||||20191107134803|ALEVIB01|ORM^O01|23|T|2.3|||||||||||
PID|1||1005555^^^NYU MRN^MRN||OSTRICH^DODUO||19820605|M||U|000 PARK AVE SOUTH^^NEW YORK^NY^10010^US^^^60|60|(555)555-5555^HOME^PH|||S|||999-99-9999|||U||N||||||||
PV1||O|NWSLED^^^NYULHLI^^^^^LI NW SLEEP DISORDER^^DEPID||||1447312459^DOE^MICHAEL^^^^^^EPIC^^^^PNPI~DOEM06^DOE^MICHAEL^^^^^^KID^^^^KID|1447312459^DOE^MICHAEL^^^^^^EPIC^^^^PNPI~DOEM06^DOE^MICHAEL^^^^^^KID^^^^KID|||||||||||496779945|||||||||||||||||||||||||20191107|||||||V";

            var message = new Message(sampleMessage);
            message.ParseMessage();

            string attendingDrId = message.GetValue(index);
            Assert.AreEqual(expected, attendingDrId);
        }

        [TestMethod]
        public void InvalidRepetitionTest()
        {
            var sampleMessage =
                @"MSH|^~\&|SYSTEM1|ABC|SYSTEM2||201803262027||DFT^P03|20180326202737608457|P|2.3||||||8859/1
EVN|P03|20180326202540
PID|1|0002381795|0002381795||Supermann^Peter^^^Herr||19990101|M|||Hamburgerstrasse 123^^Mimamu^BL^12345^CH||123456~123456^^CP||D|2|02|321|8.2.24.||| 
PV1||A|00004620^00001318^1318||||000123456^Superfrau^Maria W.^|^Superarzt^Anton^L|00097012345^Superarzt^Herbert^~~0009723456^Superarzt^Markus^||||||||000998765^Assistent A^ONKO^D||0087123456||||||||||||||||||||2140||O|||201905220600|201908201100|||||";

            var message = new Message(sampleMessage);
            message.ParseMessage();

            // Check for invalid repetition number
            try
            {
                var value = message.GetValue("PV1.8(2).1");
                Assert.IsNull(value);
                value = message.GetValue("PV1.8(3).1");
                Assert.IsNull(value);

                Assert.Fail("Unexpected non-exception");
            }
            catch
            {
                // Pass
            }
        }

        [TestMethod]
        public void RemoveTrailingComponentsTest_OnlyTrailingComponentsRemoved()
        {
            var message = new Message();

            var orcSegment = new Segment("ORC", new HL7Encoding());
            for (int eachField = 1; eachField <= 12; eachField++)
            {
                orcSegment.AddEmptyField();
            }

            for (int eachComponent = 1; eachComponent < 8; eachComponent++)
            {
                orcSegment.Fields(12).AddNewComponent(new Component(new HL7Encoding()));
            }

            orcSegment.Fields(12).Components(1).Value = "should not be removed";
            orcSegment.Fields(12).Components(2).Value = "should not be removed";
            orcSegment.Fields(12).Components(3).Value = "should not be removed";
            orcSegment.Fields(12).Components(4).Value = ""; // should not be removed because in between valid values
            orcSegment.Fields(12).Components(5).Value = "should not be removed";
            orcSegment.Fields(12).Components(6).Value = ""; // should be removed because trailing
            orcSegment.Fields(12).Components(7).Value = ""; // should be removed because trailing
            orcSegment.Fields(12).Components(8).Value = ""; // should be removed because trailing

            orcSegment.Fields(12).RemoveEmptyTrailingComponents();
            message.AddNewSegment(orcSegment);

            string serializedMessage = message.SerializeMessage(false);
            Assert.AreEqual(orcSegment.Fields(12).Components().Count, 5);
            Assert.AreEqual("ORC||||||||||||should not be removed^should not be removed^should not be removed^^should not be removed\r", serializedMessage);
        }

        [TestMethod]
        public void RemoveTrailingComponentsTest_RemoveAllFieldComponentsIfEmpty()
        {
            var message = new Message();

            var orcSegment = new Segment("ORC", new HL7Encoding());
            for (int eachField = 1; eachField <= 12; eachField++)
            {
                orcSegment.AddEmptyField();
            }

            for (int eachComponent = 1; eachComponent < 8; eachComponent++)
            {
                orcSegment.Fields(12).AddNewComponent(new Component(new HL7Encoding()));
                orcSegment.Fields(12).Components(eachComponent).Value = "";
            }

            orcSegment.Fields(12).RemoveEmptyTrailingComponents();
            message.AddNewSegment(orcSegment);

            string serializedMessage = message.SerializeMessage(false);
            Assert.AreEqual(orcSegment.Fields(12).Components().Count, 0);
            Assert.AreEqual("ORC||||||||||||\r", serializedMessage);
        }


        [TestMethod]
        public void AddRepeatingField()
        {
            var enc = new HL7Encoding();
            Segment PID = new Segment("PID", enc);
            Field f = new Field(enc);
            Field f1 = new Field("A", enc);
            Field f2 = new Field("B", enc);

            f.HasRepetitions = true;
            f.AddRepeatingField(f1);
            f.AddRepeatingField(f2);

            // Creates a new Field
            PID.AddNewField(f, 1);

            Message message = new Message();
            message.AddNewSegment(PID);
            var str = message.SerializeMessage(false);

            Assert.AreEqual("PID|A~B\r", str);
        }

        [TestMethod]
        public void BypassValidationParseMessage()
        {
            string sampleMessage = @"MSH|^~\&|SCA|SCA|LIS|LIS|202107300000||ORU^R01||P|2.4|||||||
PID|1|1234|1234||JOHN^DOE||19000101||||||||||||||
OBR|1|1234|1234||||20210708|||||||||||||||20210708||||||||||
OBX|1|TX|SCADOCTOR||^||||||F";

            try
            {
                var msg = new Message(sampleMessage);
                msg.ParseMessage(true);

                Assert.IsNull(msg.MessageControlID, "MessageControlID should be null");

                //just to make sure we have actually parsed the invalid MSH
                string messageType = msg.GetValue("MSH.9");
                Assert.AreEqual("ORU^R01", messageType, "Unexpected Message Type");
            }
            catch (Exception ex)
            {
                Assert.Fail("Unexpected exception", ex);
            }
        }

        [TestMethod]
        public void BypassValidationGetACK()
        {
            string sampleMessage = @"MSH|^~\&|SCA|SCA|LIS|LIS|202107300000||ORU^R01||P|2.4|||||||
PID|1|1234|1234||JOHN^DOE||19000101||||||||||||||
OBR|1|1234|1234||||20210708|||||||||||||||20210708||||||||||
OBX|1|TX|SCADOCTOR||^||||||F";

            try
            {
                var msg = new Message(sampleMessage);
                msg.ParseMessage(true);

                var ack = msg.GetACK(true);
                string sendingApp = ack.GetValue("MSH.3");
                string sendingFacility = ack.GetValue("MSH.4");
                string receivingApp = ack.GetValue("MSH.5");
                string receivingFacility = ack.GetValue("MSH.6");
                string messageType = ack.GetValue("MSH.9");

                Assert.IsNull(ack.MessageControlID, "MessageControlID should be null");
                Assert.AreEqual("LIS", sendingApp, "Unexpected Sending Application");
                Assert.AreEqual("LIS", sendingApp, "Unexpected Sending Facility");
                Assert.AreEqual("SCA", receivingApp, "Unexpected Receiving Application");
                Assert.AreEqual("SCA", receivingFacility, "Unexpected Receiving Facility");
                Assert.AreEqual("ACK", messageType, "Unexpected Message Type");
            }
            catch (Exception ex)
            {
                Assert.Fail("Unexpected exception", ex);
            }
        }

        [TestMethod]
        public void BypassValidationGetNACK()
        {
            string sampleMessage = @"MSH|^~\&|SCA|SCA|LIS|LIS|202107300000||ORU^R01||P|2.4|||||||
PID|1|1234|1234||JOHN^DOE||19000101||||||||||||||
OBR|1|1234|1234||||20210708|||||||||||||||20210708||||||||||
OBX|1|TX|SCADOCTOR||^||||||F";

            try
            {
                var msg = new Message(sampleMessage);
                msg.ParseMessage(true);

                var nack = msg.GetNACK("AE", "Unit test", true);
                string sendingApp = nack.GetValue("MSH.3");
                string sendingFacility = nack.GetValue("MSH.4");
                string receivingApp = nack.GetValue("MSH.5");
                string receivingFacility = nack.GetValue("MSH.6");
                string messageType = nack.GetValue("MSH.9");
                string code = nack.GetValue("MSA.1");
                string errorMessage = nack.GetValue("MSA.3");

                Assert.IsNull(nack.MessageControlID, "MessageControlID should be null");
                Assert.AreEqual("LIS", sendingApp, "Unexpected Sending Application");
                Assert.AreEqual("LIS", sendingApp, "Unexpected Sending Facility");
                Assert.AreEqual("SCA", receivingApp, "Unexpected Receiving Application");
                Assert.AreEqual("SCA", receivingFacility, "Unexpected Receiving Facility");
                Assert.AreEqual("ACK", messageType, "Unexpected Message Type");

                Assert.AreEqual("AE", code, "Unexpected Error Code");
                Assert.AreEqual("Unit test", errorMessage, "Unexpected Error Message");
            }
            catch (Exception ex)
            {
                Assert.Fail("Unexpected exception", ex);
            }
        }

        [TestMethod]
        public void SetValueSingleSegment()
        {
            var strValueFormat = "PID.2.1";
            var unchangedValuePath = "PID.3.1";
            var newPatientId = "1234567";
            var message = new Message(HL7_ADT);
            message.ParseMessage();

            message.SetValue(strValueFormat, newPatientId);
            
            Assert.AreEqual(newPatientId, message.GetValue(strValueFormat));
            Assert.AreEqual("454721", message.GetValue(unchangedValuePath));
        }

        [TestMethod]
        public void SetValueRepeatingSegments()
        {
            var strValueFormat = "NK1.2.1";
            var unchangedValuePath = "NK1.2.2";
            var newFamilyName = "SCHMOE";
            var message = new Message(HL7_ADT);
            message.ParseMessage();

            message.SetValue(strValueFormat, newFamilyName);

            var firstNK1ChangedValue = message.GetValue(strValueFormat);
            var firstNK1UnchangedValue = message.GetValue(unchangedValuePath);
            Assert.IsTrue(message.RemoveSegment("NK1", 0));
            var secondNk1ChangedValue = message.GetValue(strValueFormat);
            var secondNk1UnchangedValue = message.GetValue(unchangedValuePath);

            Assert.AreEqual(newFamilyName, firstNK1ChangedValue);
            Assert.AreEqual(newFamilyName, secondNk1ChangedValue);
            Assert.AreEqual("MARIE", firstNK1UnchangedValue);
            Assert.AreEqual("JHON", secondNk1UnchangedValue);
        }

        [TestMethod]
        public void SetValueUnavailableComponents()
        {
            var sampleMessage = @"MSH|^~\&|SYSTEM1|ABC|SYSTEM2||201803262027||DFT^P03|20180326202737608457|P|2.3|||"
                                    +"\nPID|1|0002381795|0002381795||Supermann^Peter^^^Herr||19990101|M|||";
            var testValue = "test";
            var message = new Message(sampleMessage);
            message.ParseMessage();

            var invalidRequest = Assert.ThrowsException<HL7Exception>(() => message.SetValue("", testValue));
            Assert.IsTrue(invalidRequest.Message.Contains("Request format"), "Should have thrown exception because of invalid request format");
            
            var unavailableSegment = Assert.ThrowsException<HL7Exception>(() => message.SetValue("OBX.1", testValue));
            Assert.IsTrue(unavailableSegment.Message.Contains("Segment name"), "Should have thrown exception because of unavailable Segment");
            
            var segmentLevel = Assert.ThrowsException<HL7Exception>(() => message.SetValue("PID.30", testValue));
            Assert.IsTrue(segmentLevel.Message.Contains("Field not available"), "Should have thrown exception because of unavailable Field");
            
            var componentLevel = Assert.ThrowsException<HL7Exception>(() => message.SetValue("PID.3.7", testValue));
            Assert.IsTrue(componentLevel.Message.Contains("Component not available"), "Should have thrown exception because of unavailable Component");
            
            var subComponentLevel = Assert.ThrowsException<HL7Exception>(() => message.SetValue("PID.3.1.2", testValue));
            Assert.IsTrue(subComponentLevel.Message.Contains("SubComponent not available"), "Should have thrown exception because of unavailable SubComponent");
        }

        [TestMethod]
        public void SetValueAvailableComponents()
        {
            var testValue = "test";
            var message = new Message(HL7_ADT);
            message.ParseMessage();

            Assert.IsTrue(message.SetValue("PID.1", testValue), "Should have successfully set value of Field");
            Assert.AreEqual(testValue, message.GetValue("PID.1"));
            
            Assert.IsTrue(message.SetValue("PID.2.2", testValue), "Should have successfully set value of Component");
            Assert.AreEqual(testValue, message.GetValue("PID.2.2"));
            
            Assert.IsTrue(message.SetValue("PID.2.4.1", testValue), "Should have successfully set value of SubComponent");
            Assert.AreEqual(testValue, message.GetValue("PID.2.4.1"));
        }

        [TestMethod]
        public void MessageIsComponentized()
        {
            string sampleMessage = this.HL7_ADT;
            var message = new Message(sampleMessage);
            message.ParseMessage();

            Assert.IsTrue(message.IsComponentized("PID.5"));
        }

        [TestMethod]
        public void FieldHasRepetitions()
        {
            string sampleMessage = this.HL7_ADT;
            var message = new Message(sampleMessage);
            message.ParseMessage();
            Console.WriteLine(message.Segments("PID")[0].Fields(18).Value);
            Assert.IsFalse(message.HasRepetitions("PID.3"));
            //Assert.IsTrue(message.HasRepetitions("PID.18")); // Possible bug in the upstream library
        }

        [TestMethod]
        public void SequenceNo()
        {
            string sampleMessage = @"MSH|^~\&|MEDAT|AESCU|IXZENT||20210714122713|MEDAT_KC|ORU^R01|6227281|P|2.3|||||||
PID|||||Kuh^Klarabella||19901224|F|||Bogus St^^Gotham^^42069^|||||||||||||
PV1||2|Jenkins||||||||||||||||AP0999923||P|||||||
ORC||0490000001|0490000001||CM||||202107141056
OBR|1|0490000001|0490000001|LH^LH^FN|||202107141056|20210714122713||||||202107141056|1||||||||||F|
OBX|1|NM|LH^LH^FN||20.00|mIU/ml|||||F||
NTE|1||      Follikelphase:    2.4 -  12.6 mIU/ml
NTE|2||      Ovulationsphase: 14.0 - 95.6 mIU/ml
NTE|3||      Lutealphase:      1.0 - 11.4 mIU/ml
NTE|4||      Postmenopause:    7.7 - 58.5 mIU/ml
OBR|2|0490000001|0490000001|FSH^FSH^FN|||202107141056|20210714122713||||||202107141056|1||||||||||F|
OBX|1|NM|FSH^FSH^FN||30.00|mIU/ml|||||F||
NTE|1||      Follikelphase:    3.5 - 12.5 mIU/ml
NTE|2||      Ovulationsphase:  4.7 - 21.5 mIU/ml
NTE|3||      Lutealphase:       1.7 - 7.7 mIU/ml
NTE|4||      Postmenopause:  25.8 - 134.8 mIU/ml";
            var message = new Message(sampleMessage);
            message.ParseMessage();

            var nte = message.Segments("NTE")[0];
            Assert.AreEqual(6, nte.GetSequenceNo());

            nte = message.Segments("NTE")[4];
            Assert.AreEqual(12, nte.GetSequenceNo());
        }
    }
}
