using Fluxor;

namespace TeamHubConnect.Blazor.Store.Auth;

public static class AuthReducers
{
    [ReducerMethod]
    public static AuthState ReduceLoginAction(AuthState state, LoginAction action) =>
        state with { IsLoading = true, ErrorMessage = null };

    [ReducerMethod]
    public static AuthState ReduceLoginSuccessAction(AuthState state, LoginSuccessAction action) =>
        state with 
        { 
            IsLoading = false,
            IsAuthenticated = true,
            Token = action.Token,
            CurrentUser = action.User,
            TokenExpiry = action.TokenExpiry,
            ErrorMessage = null
        };

    [ReducerMethod]
    public static AuthState ReduceLoginFailureAction(AuthState state, LoginFailureAction action) =>
        state with 
        { 
            IsLoading = false,
            IsAuthenticated = false,
            Token = null,
            CurrentUser = null,
            TokenExpiry = null,
            ErrorMessage = action.ErrorMessage
        };

    [ReducerMethod]
    public static AuthState ReduceLogoutAction(AuthState state, LogoutAction action) =>
        new AuthState();

    [ReducerMethod]
    public static AuthState ReduceLoadUserAction(AuthState state, LoadUserAction action) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static AuthState ReduceLoadUserSuccessAction(AuthState state, LoadUserSuccessAction action) =>
        state with 
        { 
            IsLoading = false,
            CurrentUser = action.User,
            IsAuthenticated = true
        };

    [ReducerMethod]
    public static AuthState ReduceLoadUserFailureAction(AuthState state, LoadUserFailureAction action) =>
        state with 
        { 
            IsLoading = false,
            ErrorMessage = action.ErrorMessage
        };

    [ReducerMethod]
    public static AuthState ReduceUpdateUserStatusAction(AuthState state, UpdateUserStatusAction action) =>
        state with 
        { 
            CurrentUser = state.CurrentUser with 
            { 
                Status = action.Status,
                StatusMessage = action.Message
            }
        };

    [ReducerMethod]
    public static AuthState ReduceClearErrorAction(AuthState state, ClearErrorAction action) =>
        state with { ErrorMessage = null };
}