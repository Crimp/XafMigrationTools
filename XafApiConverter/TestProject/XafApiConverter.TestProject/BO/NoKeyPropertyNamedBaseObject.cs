using System;
using System.Collections.Generic;
using System.Text;
using DevExpress.Xpo;
using DevExpress.Persistent.Base;
using System.ComponentModel;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Data.Filtering;
using DevExpress.Xpo.DB.Exceptions;
using System.Threading;

namespace FeatureCenter.Module.KeyProperty {
	[NonPersistent]
	public class NoKeyPropertyNamedBaseObject : XPBaseObject {
		private string name;
		public NoKeyPropertyNamedBaseObject(Session session) : base(session) { }
		public string Name {
			get { return name; }
			set { SetPropertyValue(nameof(Name), ref name, value); }
		}
	}
}
