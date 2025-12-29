using DevExpress.Xpo;
using FeatureCenter.Module.KeyProperty;

namespace Test.Module.Controllers {
    public class TestController_1 {
        public StringKeyPropertyObject CreateStringKeyPropertyObject(Session session) {
            return new StringKeyPropertyObject(session);
        }
    }
    public class TestController_2 {
        StringKeyPropertyObject stringKeyPropertyObject;
        public void Init(Session session) {
            stringKeyPropertyObject = new StringKeyPropertyObject(session);
        }
    }
}
