using System;
using Messanger;
using NUnit.Framework;


namespace MessangerTest
{
    [TestFixture,Category("Unit")]
    public class MessangerTest
    {
        [Test, Category("unit")]
        public void RegisterAndSend()
        {
            //Given
            IMessanger messanger = Messanger.Messanger.GetInstance();
            const string expected = "some text";
            string result = null;
            //When
            messanger.Register(typeof(SimpleMessage), obj =>
            {
                var message = obj as SimpleMessage;
                result = message.Value ?? "";
            });
            messanger.Send(new SimpleMessage(expected));
            //Then
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void RegisterMoreThanOneObserver()
        {
            //Given
            IMessanger messanger = Messanger.Messanger.GetInstance();
            const string expected = "some text";
            string result1 = null;
            string result2 = null;
            //When
            messanger.Register(typeof(SimpleMessage), obj =>
            {
                var message = obj as SimpleMessage;
                result1 = message.Value ?? "";
            });
            messanger.Register(typeof(SimpleMessage), obj =>
            {
                var message = obj as SimpleMessage;
                result2 = message.Value ?? "";
            });
            messanger.Send(new SimpleMessage(expected));
            //Then
            Assert.That(result1, Is.EqualTo(expected));
            Assert.That(result2, Is.EqualTo(expected));
        }

        [Test]
        public void SendWrongMessage()
        {
            //Given
            IMessanger messanger = Messanger.Messanger.GetInstance();
            string result = "";
            //When
            messanger.Register(typeof(SimpleMessage), obj =>
            {
                var message = obj as SimpleMessage;
                if(message != null)
                    result = message.Value ?? "";
            });
            messanger.Send(new Object());
            //Then
            Assert.That(result, Is.Empty);
        }
    }


    public class SimpleMessage
    {
        public SimpleMessage(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}