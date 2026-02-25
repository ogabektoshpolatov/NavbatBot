namespace bot.Models;

public class CB
{
    // Task asosiy
    public static string Task(int taskId) => $"task:{taskId}";
    public static string TaskViewUsers(int taskId) => $"task:{taskId}:viewUsers";
    public static string TaskAddUser(int taskId) => $"task:{taskId}:addUser";
    public static string TaskRemoveUser(int taskId) => $"task:{taskId}:removeUser";
    public static string TaskAssignQueue(int taskId) => $"task:{taskId}:assignUserToQueue";
    public static string TaskSkipQueue(int taskId) => $"task:{taskId}:skipQueue";

    // Invite
    public static string TaskInvite(int taskId) => $"task:{taskId}:invite";
    public static string TaskInviteRefresh(int taskId) => $"task:{taskId}:invite:refresh";
    public static string TaskInviteToggle(int taskId) => $"task:{taskId}:invite:toggle";

    // Edit menu
    public static string TaskEdit(int taskId) => $"task:{taskId}:edit";
    public static string TaskEditName(int taskId) => $"task:{taskId}:edit:name";
    public static string TaskEditDescription(int taskId) => $"task:{taskId}:edit:description";
    public static string TaskEditGroupId(int taskId) => $"task:{taskId}:edit:groupid";
    public static string TaskEditNotifyTime(int taskId) => $"task:{taskId}:edit:notifytime";
    public static string TaskEditInterval(int taskId) => $"task:{taskId}:edit:interval";
    public static string TaskEditSendToGroup(int taskId) => $"task:{taskId}:edit:sendtogroup";
    public static string TaskEditIntervalSet(int taskId, int days) => $"task:{taskId}:edit:interval:{days}";
    public static string TaskEditNotifyTimeSet(int taskId, string time) => $"task:{taskId}:edit:notifytime:{time}";

    // Navbat tartibi
    public static string TaskReorder(int taskId) => $"task:{taskId}:reorder";
    public static string TaskReorderUp(int taskId, long userId) => $"task:{taskId}:reorder:{userId}:up";
    public static string TaskReorderDown(int taskId, long userId) => $"task:{taskId}:reorder:{userId}:down";
    public static string TaskReorderSave(int taskId) => $"task:{taskId}:reorder:save";

    // Join
    public static string JoinConfirm(int taskId) => $"join:{taskId}:confirm";
    public static string JoinCancel(int taskId) => $"join:{taskId}:cancel";

    // Delete
    public static string TaskDelete(int taskId) => $"task:{taskId}:delete";
    public static string TaskDeleteConfirm(int taskId) => $"task:{taskId}:delete:confirm";
}