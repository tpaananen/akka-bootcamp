using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTail
{
    #region Neutral/system messages

    public class ContinueProcessingMessage
    {
    }

    public abstract class InputMessage
    {
        protected InputMessage(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }
    }

    #endregion

    #region Success messages

    public class InputSuccessMessage : InputMessage
    {
        public InputSuccessMessage(string reason)
            : base(reason)
        {
        }
    }

    #endregion

    #region Error messages

    public class InputErrorMessage : InputMessage
    {
        public InputErrorMessage(string reason)
            : base(reason)
        {
        }
    }

    public class NullInputErrorMessage : InputErrorMessage
    {
        public NullInputErrorMessage(string reason)
            : base(reason)
        {
        }
    }

    public class ValidationErrorMessage : InputErrorMessage
    {
        public ValidationErrorMessage(string reason)
            : base(reason)
        {
        }
    }

    #endregion

}
