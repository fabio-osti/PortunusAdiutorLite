namespace PortunusAdiutor.Exceptions;

/// <summary>
/// 	Represents error that occur when an user that 
/// 	already exists is created.
/// </summary>
public class UserAlreadyExistsException : PortunusException
{
	/// <summary>
	/// 	Initializes the exception.
	/// </summary>
	public UserAlreadyExistsException() 
		: base("This user already exists.") { }
}