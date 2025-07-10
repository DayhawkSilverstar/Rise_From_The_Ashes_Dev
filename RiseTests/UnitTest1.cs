using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace RiseTests
{
    [TestClass]
    public class TimeCheck
    {
        [TestMethod]
        public void ConvertULongToTime()
        {            
            Assert.AreEqual(Convert.ToUInt64(377000), DynamicEventManager.Instance.GetTicksFromDate(16, 17, 0));            
            Assert.AreEqual(Convert.ToUInt64(219800), DynamicEventManager.Instance.GetTicksFromDate(10, 3, 48));
        }

        [TestMethod]
        public void ConvertTicksToTime()
        {
            Assert.AreEqual("16 17:0", DynamicEventManager.Instance.GetDateFromTicks(377000));
            Assert.AreEqual("10 3:48", DynamicEventManager.Instance.GetDateFromTicks(219800));
        }

        [TestMethod]
        public void ExecuteMinEventAction()
        {
            EventBundle bundle = new EventBundle();
            bundle.EventAction = new MinEventActionAddChatMessage();
            bundle.Params = new MinEventParams();

            if (bundle.EventAction.CanExecute(MinEventTypes.onSelfBuffStart, bundle.Params))
            {
                bundle.EventAction.Execute(bundle.Params);
            }
        }
    }
}
