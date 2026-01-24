namespace CentroidAPI
{
    public class CNCPipe
    {
        // Nested types (stubs)
        public class Axis
        {
            public enum Direction { Positive, Negative }
            public enum Rate { Slow, Medium, Fast }
            public Axis(CNCPipe? parent = null) { }
            public ReturnCode GetAccelTime(Axes axis, out double accel_time) { accel_time = 0; System.Console.WriteLine($"[Mock] GetAccelTime({axis}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetAxisReversal(Axes axis, out bool is_axis_reversed) { is_axis_reversed = false; System.Console.WriteLine($"[Mock] GetAxisReversal({axis}, out bool)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetCountsPerTurn(Axes axis, out double counts_per_turn) { counts_per_turn = 0; System.Console.WriteLine($"[Mock] GetCountsPerTurn({axis}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetDeadstartVelocity(Axes axis, out double deadstart_velocity) { deadstart_velocity = 0; System.Console.WriteLine($"[Mock] GetDeadstartVelocity({axis}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetDeltaVMax(Axes axis, out double delta_v_max) { delta_v_max = 0; System.Console.WriteLine($"[Mock] GetDeltaVMax({axis}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetHomeLimit(Axes axis, Direction direction, out double home_limit) { home_limit = 0; System.Console.WriteLine($"[Mock] GetHomeLimit({axis}, {direction}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetLabel(Axes axis, out char label) { label = 'X'; System.Console.WriteLine($"[Mock] GetLabel({axis}, out char)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetLashComp(Axes axis, out double lash_comp) { lash_comp = 0; System.Console.WriteLine($"[Mock] GetLashComp({axis}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetLimit(Axes axis, Direction direction, out double axis_limit) { axis_limit = 0; System.Console.WriteLine($"[Mock] GetLimit({axis}, {direction}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetPower(Axes axis, out MotorPower power_state) { power_state = MotorPower.Off; System.Console.WriteLine($"[Mock] GetPower({axis}, out MotorPower)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetRate(Axes axis, Rate jog_rate, out double rate) { rate = 0; System.Console.WriteLine($"[Mock] GetRate({axis}, {jog_rate}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetScalesCounts(Axes axis, out double scaling_counts) { scaling_counts = 0; System.Console.WriteLine($"[Mock] GetScalesCounts({axis}, out double)"); return ReturnCode.SUCCESS; }
            // Add more methods as needed from documentation
            public enum MotorPower { Off, On }
        }
        public class Csr
        {
            public Csr(CNCPipe? parent = null) { }
            public ReturnCode DisableCSR() { System.Console.WriteLine("[Mock] DisableCSR()"); return ReturnCode.SUCCESS; }
            public ReturnCode GetAngle(out double angle) { angle = 0; System.Console.WriteLine("[Mock] GetAngle(out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetAngle(int wcs, out double csr_angle) { csr_angle = 0; System.Console.WriteLine($"[Mock] GetAngle({wcs}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode ReenableCSR() { System.Console.WriteLine("[Mock] ReenableCSR()"); return ReturnCode.SUCCESS; }
            public ReturnCode SetAngle(double angle) { System.Console.WriteLine($"[Mock] SetAngle({angle})"); return ReturnCode.SUCCESS; }
            public ReturnCode SetAngle(int wcs, double angle) { System.Console.WriteLine($"[Mock] SetAngle({wcs}, {angle})"); return ReturnCode.SUCCESS; }
        }
        public class Dro
        {
            public Dro(CNCPipe? parent = null) { }
            public ReturnCode GetDroValue(int dro_num, out double dro_value) { dro_value = 0; System.Console.WriteLine($"[Mock] GetDroValue({dro_num}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode SetDroValue(int dro_num, double dro_value) { System.Console.WriteLine($"[Mock] SetDroValue({dro_num}, {dro_value})"); return ReturnCode.SUCCESS; }
        }
        public class InboundComm { public class CommPacket { } }
        public class Job
        {
            public Job(CNCPipe? parent = null) { }
            public ReturnCode StartJob(string jobName) { System.Console.WriteLine($"[Mock] StartJob({jobName})"); return ReturnCode.SUCCESS; }
            public ReturnCode StopJob() { System.Console.WriteLine("[Mock] StopJob()"); return ReturnCode.SUCCESS; }
            public ReturnCode GetJobStatus(out string status) { status = "Idle"; System.Console.WriteLine("[Mock] GetJobStatus(out string)"); return ReturnCode.SUCCESS; }
            public ReturnCode RunCommand(string gcode) { System.Console.WriteLine($"[Mock] RunCommand({gcode})"); return ReturnCode.SUCCESS; }
        }
        public class MessageWindow { }
        public class Parameter
        {
            public Parameter(CNCPipe? parent = null) { }
            public ReturnCode GetMachineParameterValue(int parameter_num, out double parameter_value)
            {
                parameter_value = 0.0;
                System.Console.WriteLine($"[Mock] GetMachineParameterValue({parameter_num}, out double)");
                return ReturnCode.SUCCESS;
            }
            public ReturnCode SetMachineParameter(int parameter_num, double value)
            {
                System.Console.WriteLine($"[Mock] SetMachineParameter({parameter_num}, {value})");
                return ReturnCode.SUCCESS;
            }
            public ReturnCode GetParameterName(int parameter_num, out string name) { name = $"Param{parameter_num}"; System.Console.WriteLine($"[Mock] GetParameterName({parameter_num}, out string)"); return ReturnCode.SUCCESS; }
        }
        public class Plc
        {
            public Plc(CNCPipe? parent = null) { }
            public ReturnCode GetPlcBit(int bit_num, out bool value) { value = false; System.Console.WriteLine($"[Mock] GetPlcBit({bit_num}, out bool)"); return ReturnCode.SUCCESS; }
            public ReturnCode SetPlcBit(int bit_num, bool value) { System.Console.WriteLine($"[Mock] SetPlcBit({bit_num}, {value})"); return ReturnCode.SUCCESS; }
        }
        public class Screen
        {
            public Screen(CNCPipe? parent = null) { }
            public ReturnCode GetScreenInfo(out string info) { info = "MockScreen"; System.Console.WriteLine("[Mock] GetScreenInfo(out string)"); return ReturnCode.SUCCESS; }
        }
        public class State
        {
            public State(CNCPipe? parent = null) { }
            public ReturnCode GetFeedRate(out double feedrate) { feedrate = 0; System.Console.WriteLine("[Mock] GetFeedRate(out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetSpindleSpeed(out double speed) { speed = 0; System.Console.WriteLine("[Mock] GetSpindleSpeed(out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode GetStateInfo(out string info) { info = "Idle"; System.Console.WriteLine("[Mock] GetStateInfo(out string)"); return ReturnCode.SUCCESS; }
        }
        public class Sys
        {
            public Sys(CNCPipe? parent = null) { }
            public ReturnCode GetSystemInfo(out string info) { info = "MockSystem"; System.Console.WriteLine("[Mock] GetSystemInfo(out string)"); return ReturnCode.SUCCESS; }
        }
        public class Tool
        {
            public Tool(CNCPipe? parent = null) { }
            public ReturnCode GetToolNumber(out int toolNum) { toolNum = 0; System.Console.WriteLine("[Mock] GetToolNumber(out int)"); return ReturnCode.SUCCESS; }
            public ReturnCode SetToolNumber(int toolNum) { System.Console.WriteLine($"[Mock] SetToolNumber({toolNum})"); return ReturnCode.SUCCESS; }
        }
        public class Wcs
        {
            public Wcs(CNCPipe? parent = null) { }
            public ReturnCode GetWcsOffset(int wcsNum, out double offset) { offset = 0; System.Console.WriteLine($"[Mock] GetWcsOffset({wcsNum}, out double)"); return ReturnCode.SUCCESS; }
            public ReturnCode SetWcsOffset(int wcsNum, double offset) { System.Console.WriteLine($"[Mock] SetWcsOffset({wcsNum}, {offset})"); return ReturnCode.SUCCESS; }
        }
        // Visual Basic versions (stubs)
        public class VB_Axis { }
        public class VB_Csr { }
        public class VB_Dro { }
        public class VB_InboundComm { }
        public class VB_Job { }
        public class VB_MessageWindow { }
        public class VB_Parameter { }
        public class VB_Plc { }
        public class VB_Screen { }
        public class VB_State { }
        public class VB_System { }
        public class VB_Tool { }
        public class VB_Wcs { }

        // Enums
        public enum Axes { AXIS_1, AXIS_2, AXIS_3, AXIS_4, AXIS_5, AXIS_6, AXIS_7, AXIS_8 }
        public enum ReturnCode
        {
            ERROR_CLIENT_LOCKED, ERROR_INVALID_ARGUMENT, ERROR_INVALID_AXIS, ERROR_INVALID_PLC_BIT_NUMBER,
            ERROR_INVALID_PLC_BIT_TYPE, ERROR_INVALID_REQUEST, ERROR_INVALID_SKINNING_DATA_WORD_INDEX, ERROR_JOB_IN_PROGRESS,
            ERROR_PIPE_IS_BROKEN, ERROR_PLC_SEND_SKINNING_DATA, ERROR_SAVE_CONFIGURATION, ERROR_SEND_COMMAND,
            ERROR_SEND_PID_SETUPS, ERROR_SEND_SETUPS, ERROR_OUT_OF_RANGE, ERROR_CONVERSION, ERROR_UNKNOWN,
            ERROR_VALIDATION, STATUS_UNKNOWN, ERROR_DEPRECATED, ERROR_LICENSE_GENERAL_FAILURE, ERROR_LICENSE_MISMATCHED_VERSIONS,
            ERROR_LICENSE_MISMATCHED_SERIAL_NUMBER, ERROR_LICENSE_LOCKED, ERROR_INVALID_RETURN, ERROR_EXPERIMENTAL_FEATURE, SUCCESS
        }

        // Public fields/properties (stubs)
        public Axis axis = new Axis();
        public Csr csr = new Csr();
        public Dro dro = new Dro();
        public Job job = new Job();
        public MessageWindow message_window = new MessageWindow();
        public Parameter parameter = new Parameter();
        public Plc plc = new Plc();
        public Screen screen = new Screen();
        public State state = new State();
        public Sys system = new Sys();
        public Tool tool = new Tool();
        public Wcs wcs = new Wcs();
        public InboundComm inbound_communications = new InboundComm();

        // Constructors
        public CNCPipe() { }
        public CNCPipe(int timeout) { }
        public CNCPipe(bool useVcpPipe, int timeout) { }

        // Methods
        public bool IsConstructed() => true;

        // Events (stubs)
        public event System.Action? MessageReceived;
        protected virtual void OnMessageReceived(InboundComm.CommPacket data) { MessageReceived?.Invoke(); }
    }
}
