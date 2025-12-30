using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafApiConverter.TestProject.BO {
    // TODO: The 'Message' class has been marked automatically due to usage of types that have no XAF for .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF for .NET
    // NOTE:
    //   - Type 'DevExpress.Persistent.BaseImpl.Note' has no equivalent in XAF for .NET
    //     Note has no equivalent in XAF for .NET (loaded from removed-api.txt)
    public class Message : Note {
        public Message(Session session) : base(session) { }
    }
}
