<template>
  <div class="groupchat-container">
    <!-- 聊天室顶部区域 -->
    <div class="groupchat-header">
      <div class="room-info">
        <h1 class="room-name">{{ currentGroup ? currentGroup.groupName : '群组聊天' }}</h1>
        <div class="user-count">
          <span class="material-icon">group</span>
          <span>{{ currentGroup ? currentGroup.memberCount : 0 }} 名成员</span>
        </div>
      </div>
      <div class="user-actions">
        <div class="current-user">
          <span class="user-avatar">
            <img v-if="userAvatar" :src="userAvatar" :alt="username">
            <span v-else class="default-avatar">{{ username.charAt(0).toUpperCase() }}</span>
          </span>
          <span class="username">{{ username }}</span>
        </div>
        <div class="group-actions">
          <button @click="showCreateGroupModal = true" class="action-btn">
            <span class="material-icon">add</span>
            <span>创建群组</span>
          </button>
          <button @click="showAddUserModal = true" class="action-btn" v-if="currentGroup">
            <span class="material-icon">person_add</span>
            <span>添加成员</span>
          </button>
          <button @click="showRemoveUserModal = true" class="action-btn" v-if="currentGroup">
            <span class="material-icon">person_remove</span>
            <span>移除成员</span>
          </button>
          <button @click="switchToChatRoom" class="action-btn">
            <span class="material-icon">forum</span>
            <span>公共聊天室</span>
          </button>
        </div>
      </div>
    </div>

    <!-- 聊天内容和群组列表区域 -->
    <div class="groupchat-content">
      <!-- 群组列表 -->
      <div class="groups-panel">
        <div class="panel-header">
          <h3>我的群组</h3>
        </div>
        <div class="groups-list">
          <div 
            v-for="group in groups" 
            :key="group.groupId" 
            class="group-item"
            :class="{ 'active': currentGroup && currentGroup.groupId === group.groupId }"
            @click="selectGroup(group)"
          >
            <div class="group-avatar">
              <span class="default-avatar">{{ group.groupName.charAt(0).toUpperCase() }}</span>
            </div>
            <div class="group-details">
              <span class="group-name">{{ group.groupName }}</span>
              <span class="group-members">{{ group.memberCount }} 名成员</span>
            </div>
          </div>
          
          <div v-if="groups.length === 0" class="empty-groups">
            <span class="material-icon">groups</span>
            <p>暂无群组</p>
          </div>
        </div>
      </div>

      <!-- 聊天消息区域 -->
      <div class="chat-messages" ref="messagesContainer">
        <div class="messages-list">
          <div v-if="!currentGroup" class="empty-state">
            <span class="material-icon">chat</span>
            <p>请选择一个群组开始聊天</p>
          </div>
          <div v-else-if="messages.length === 0" class="empty-state">
            <span class="material-icon">chat</span>
            <p>还没有任何消息，开始聊天吧！</p>
          </div>

          <div 
            v-for="message in messages" 
            :key="message.messageId" 
            :class="['message-item', {
              'my-message': message.senderId === userId,
              'system-message': message.messageType === 'system'
            }]"
          >
            <!-- 系统消息 -->
            <div v-if="message.messageType === 'system'" class="system-message-content">
              {{ message.content }}
            </div>
            
            <!-- 普通用户消息 -->
            <template v-else>
              <!-- 其他人的消息显示头像和名字 -->
              <div v-if="message.senderId !== userId" class="message-sender">
                <div class="sender-avatar">
                  <span class="default-avatar">{{ message.senderName.charAt(0).toUpperCase() }}</span>
                </div>
                <div class="message-info">
                  <div class="sender-name">{{ message.senderName }}</div>
                  <div class="message-time">{{ formatTime(message.createTime) }}</div>
                </div>
              </div>
              
              <div class="message-content">
                {{ message.content }}
              </div>
              
              <!-- 自己的消息右侧显示时间 -->
              <div v-if="message.senderId === userId" class="my-message-time">
                {{ formatTime(message.createTime) }}
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- 聊天输入区域 -->
    <div class="chat-input-area" v-if="currentGroup">
      <div class="input-container">
        <textarea 
          v-model="message" 
          @keydown.enter.prevent="sendMessage"
          placeholder="输入消息..." 
          :disabled="!isConnected"
          ref="messageInput"
        ></textarea>
        <button 
          class="send-button" 
          @click="sendMessage" 
          :disabled="!isConnected || !message.trim()"
        >
          <span class="material-icon">send</span>
        </button>
      </div>
      <div class="connection-status" :class="isConnected ? 'connected' : 'disconnected'">
        <span class="status-indicator"></span>
        <span>{{ isConnected ? '已连接' : '连接中...' }}</span>
      </div>
    </div>

    <!-- 创建群组模态框 -->
    <div class="modal" v-if="showCreateGroupModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>创建新群组</h3>
          <button @click="showCreateGroupModal = false" class="close-btn">
            <span class="material-icon">close</span>
          </button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>群组ID</label>
            <input v-model="newGroup.groupId" type="number" placeholder="输入群组ID">
          </div>
          <div class="form-group">
            <label>群组名称</label>
            <input v-model="newGroup.groupName" type="text" placeholder="输入群组名称">
          </div>
          <div class="form-group">
            <label>群组描述</label>
            <textarea v-model="newGroup.description" placeholder="输入群组描述"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button @click="showCreateGroupModal = false" class="cancel-btn">取消</button>
          <button @click="createGroup" class="confirm-btn">创建</button>
        </div>
      </div>
    </div>

    <!-- 添加用户模态框 -->
    <div class="modal" v-if="showAddUserModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>添加成员</h3>
          <button @click="showAddUserModal = false" class="close-btn">
            <span class="material-icon">close</span>
          </button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>用户ID</label>
            <input v-model="newMemberId" type="number" placeholder="输入要添加的用户ID">
          </div>
        </div>
        <div class="modal-footer">
          <button @click="showAddUserModal = false" class="cancel-btn">取消</button>
          <button @click="addUserToGroup" class="confirm-btn">添加</button>
        </div>
      </div>
    </div>

    <!-- 移除用户模态框 -->
    <div class="modal" v-if="showRemoveUserModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>移除成员</h3>
          <button @click="showRemoveUserModal = false" class="close-btn">
            <span class="material-icon">close</span>
          </button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>用户ID</label>
            <input v-model="removeMemberId" type="number" placeholder="输入要移除的用户ID">
          </div>
        </div>
        <div class="modal-footer">
          <button @click="showRemoveUserModal = false" class="cancel-btn">取消</button>
          <button @click="removeUserFromGroup" class="confirm-btn">移除</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import axios from 'axios';

export default {
  name: 'GroupChatView',
  data() {
    return {
      connection: null,
      groups: [],
      currentGroup: null,
      messages: [],
      message: '',
      isConnected: false,
      isLoading: true,
      error: null,
      userId: null,
      username: '',
      userAvatar: null,
      showCreateGroupModal: false,
      showAddUserModal: false,
      showRemoveUserModal: false,
      newGroup: {
        groupId: null,
        groupName: '',
        description: '',
        creatorId: null,
        memberCount: 1
      },
      newMemberId: '',
      removeMemberId: '',
      previousGroupId: null,
      groupUsers: []
    }
  },
  async created() {
    // 从localStorage获取用户信息
    this.userId = parseInt(localStorage.getItem('userId'));
    this.username = localStorage.getItem('userUsername');
    this.userAvatar = localStorage.getItem('userAvatar');
    this.newGroup.creatorId = this.userId;
    
    // 检查是否已登录
    const token = localStorage.getItem('token');
    if (!token) {
      this.$router.push('/login');
      return;
    }
    
    // 获取用户群组列表
    await this.loadUserGroups();
    
    // 初始化SignalR连接
    await this.initSignalRConnection();
    
    // 设置页面标题
    document.title = '群组聊天 | WHU-Chat';
  },
  mounted() {
    // 消息框自动滚动到底部
    this.scrollToBottom();
    
    // 监听窗口大小变化以保持滚动到底部
    window.addEventListener('resize', this.scrollToBottom);
  },
  beforeUnmount() {
    // 断开SignalR连接
    if (this.connection) {
      this.connection.stop();
    }
    
    // 移除事件监听
    window.removeEventListener('resize', this.scrollToBottom);
  },
  methods: {
    // 加载用户群组列表
    async loadUserGroups() {
      try {
        const response = await axios.get(`/api/group/user/${this.userId}`);
        if (response.data.code === 200) {
          this.groups = response.data.data;
        }
      } catch (error) {
        console.error('获取群组列表失败:', error);
      }
    },
    
    // 选择群组
    async selectGroup(group) {
      this.currentGroup = group;
      this.messages = [];
      
      // 获取群组历史消息
      try {
        const response = await axios.get(`/api/group/${group.groupId}/messages?count=20`);
        if (response.data.code === 200) {
          // 获取群组用户列表
          const usersResponse = await axios.get(`/api/group/${group.groupId}/users`);
          if (usersResponse.data.code === 200) {
            this.groupUsers = usersResponse.data.data;
            // 处理消息，添加发送者名称，并按时间升序排序
            this.messages = response.data.data
              .map(message => {
                const sender = this.groupUsers.find(user => user.id === message.senderId);
                return {
                  ...message,
                  senderName: sender ? sender.username : '未知用户'
                };
              })
              .sort((a, b) => new Date(a.createTime) - new Date(b.createTime));
          }
          this.$nextTick(this.scrollToBottom);
        }
      } catch (error) {
        console.error('获取历史消息失败:', error);
        this.showError('获取历史消息失败: ' + error.response?.data?.message || error.message);
      }
      
      // 加入SignalR群组
      if (this.connection && this.isConnected) {
        try {
          // 先离开之前的群组
          if (this.previousGroupId) {
            await this.connection.invoke('LeaveGroup', this.previousGroupId);
          }
          
          // 加入新群组
          await this.connection.invoke('JoinGroup', 
            this.userId,
            this.username,
            group.groupId
          );
          
          // 记录当前群组ID
          this.previousGroupId = group.groupId;
        } catch (error) {
          console.error('加入群组失败:', error);
          this.showError('加入群组失败，请稍后重试');
        }
      }
    },
    
    // 创建群组
    async createGroup() {
      try {
        if (!this.newGroup.groupId) {
          this.showError('请输入群组ID');
          return;
        }
        const response = await axios.post('/api/group/create', this.newGroup);
        if (response.data.code === 200) {
          // 重新加载群组列表
          await this.loadUserGroups();
          this.showCreateGroupModal = false;
          this.newGroup = {
            groupId: null,
            groupName: '',
            description: '',
            creatorId: this.userId,
            memberCount: 1
          };
        }
      } catch (error) {
        console.error('创建群组失败:', error);
        this.showError('创建群组失败: ' + error.response?.data?.message || error.message);
      }
    },
    
    // 添加用户到群组
    async addUserToGroup() {
      if (!this.currentGroup || !this.newMemberId) return;
      
      try {
        const response = await axios.post(`/api/group/${this.currentGroup.groupId}/add-user/${this.newMemberId}`);
        if (response.data.code === 200) {
          // 更新群组信息
          const groupResponse = await axios.get(`/api/group/${this.currentGroup.groupId}`);
          if (groupResponse.data.code === 200) {
            this.currentGroup = groupResponse.data.data;
          }
          this.showAddUserModal = false;
          this.newMemberId = '';
        }
      } catch (error) {
        console.error('添加用户到群组失败:', error);
      }
    },
    
    // 从群组移除用户
    async removeUserFromGroup() {
      if (!this.currentGroup || !this.removeMemberId) return;
      
      try {
        const response = await axios.delete(`/api/group/${this.currentGroup.groupId}/remove-user/${this.removeMemberId}`);
        if (response.data.code === 200) {
          // 更新群组信息
          const groupResponse = await axios.get(`/api/group/${this.currentGroup.groupId}`);
          if (groupResponse.data.code === 200) {
            this.currentGroup = groupResponse.data.data;
          }
          this.showRemoveUserModal = false;
          this.removeMemberId = '';
        }
      } catch (error) {
        console.error('从群组移除用户失败:', error);
      }
    },
    
    // 切换到公共聊天室
    switchToChatRoom() {
      this.$router.push('/chatroom');
    },
    
    // 初始化SignalR连接
    async initSignalRConnection() {
      try {
        this.connection = new HubConnectionBuilder()
          .withUrl('http://localhost:5067/groupChatHub')
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build();
        
        // 监听接收消息
        this.connection.on('ReceiveMessage', (message) => {
          if (this.currentGroup && message.groupId === this.currentGroup.groupId) {
            // 查找发送者信息
            const sender = this.groupUsers.find(user => user.id === message.senderId);
            const messageWithSender = {
              ...message,
              senderName: sender ? sender.username : '未知用户'
            };
            console.log('接收到的消息:', messageWithSender);
            this.messages.push(messageWithSender);
            // 保持消息按时间升序排序
            this.messages.sort((a, b) => new Date(a.createTime) - new Date(b.createTime));
            this.$nextTick(this.scrollToBottom);
          }
        });

        // 监听用户加入群组
        this.connection.on('UserJoinedGroup', (groupId, username) => {
          if (this.currentGroup && groupId === this.currentGroup.groupId) {
            this.messages.push({
              messageId: Date.now(),
              messageType: 'system',
              content: `${username} 加入了群组`,
              createTime: new Date()
            });
            this.$nextTick(this.scrollToBottom);
          }
        });

        // 监听用户离开群组
        this.connection.on('UserLeftGroup', (groupId, username) => {
          if (this.currentGroup && groupId === this.currentGroup.groupId) {
            this.messages.push({
              messageId: Date.now(),
              messageType: 'system',
              content: `${username} 离开了群组`,
              createTime: new Date()
            });
            this.$nextTick(this.scrollToBottom);
          }
        });

        // 监听错误消息
        this.connection.on('Error', (errorMessage) => {
          console.error('SignalR错误:', errorMessage);
          this.showError(errorMessage);
        });
        
        // 启动连接
        await this.connection.start();
        this.isConnected = true;
        
      } catch (error) {
        console.error('SignalR连接失败:', error);
        this.error = '无法连接到聊天服务器，请稍后再试。';
        this.isConnected = false;
      } finally {
        this.isLoading = false;
      }
    },
    
    // 发送消息
    async sendMessage() {
      if (!this.isConnected || !this.message.trim() || !this.currentGroup) return;
      
      try {
        await this.connection.invoke('SendMessageToGroup', this.message.trim());
        this.message = '';
      } catch (error) {
        console.error('发送消息失败:', error);
        this.showError('发送消息失败，请稍后重试');
      }
    },

    // 显示错误提示
    showError(message) {
      // 这里可以实现错误提示的显示逻辑
      console.error(message);
      // 例如：使用element-ui的Message组件
      // this.$message.error(message);
    },
    
    // 格式化时间
    formatTime(timestamp) {
      try {
        if (!timestamp) {
          console.warn('时间戳为空:', timestamp);
          return '未知时间';
        }
        console.log('原始时间戳:', timestamp);
        const date = new Date(timestamp);
        if (isNaN(date.getTime())) {
          console.error('无效的时间戳:', timestamp);
          return '无效时间';
        }
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      } catch (error) {
        console.error('格式化时间出错:', error, '时间戳:', timestamp);
        return '时间错误';
      }
    },
    
    // 滚动到底部
    scrollToBottom() {
      const container = this.$refs.messagesContainer;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    }
  }
}
</script>

<style scoped>
.groupchat-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fb;
}

.groupchat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background-color: #fff;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.room-info {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.room-name {
  font-size: 1.5rem;
  font-weight: 600;
  color: #2c3e50;
}

.user-count {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #666;
}

.user-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.current-user {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  overflow: hidden;
  background-color: #e0e0e0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.user-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.default-avatar {
  font-size: 1rem;
  color: #666;
}

.group-actions {
  display: flex;
  gap: 0.5rem;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  background-color: #4285f4;
  color: white;
  cursor: pointer;
  transition: background-color 0.2s;
}

.action-btn:hover {
  background-color: #3367d6;
}

.groupchat-content {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.groups-panel {
  width: 250px;
  background-color: #fff;
  border-right: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
}

.panel-header {
  padding: 1rem;
  border-bottom: 1px solid #e0e0e0;
}

.groups-list {
  flex: 1;
  overflow-y: auto;
  padding: 0.5rem;
}

.group-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.group-item:hover {
  background-color: #f5f5f5;
}

.group-item.active {
  background-color: #e3f2fd;
}

.group-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: #e0e0e0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.group-details {
  display: flex;
  flex-direction: column;
}

.group-name {
  font-weight: 500;
}

.group-members {
  font-size: 0.875rem;
  color: #666;
}

.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
  background-color: #f5f7fb;
}

.messages-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.message-item {
  display: flex;
  flex-direction: column;
  max-width: 70%;
}

.message-item.my-message {
  align-self: flex-end;
}

.message-sender {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.25rem;
}

.sender-avatar {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background-color: #e0e0e0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.message-info {
  display: flex;
  flex-direction: column;
}

.sender-name {
  font-weight: 500;
  font-size: 0.875rem;
}

.message-time {
  font-size: 0.75rem;
  color: #666;
}

.message-content {
  padding: 0.75rem;
  border-radius: 8px;
  background-color: #fff;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.my-message .message-content {
  background-color: #e3f2fd;
}

.system-message-content {
  align-self: center;
  padding: 0.5rem 1rem;
  background-color: #f5f5f5;
  border-radius: 4px;
  color: #666;
  font-size: 0.875rem;
}

.chat-input-area {
  padding: 1rem;
  background-color: #fff;
  border-top: 1px solid #e0e0e0;
}

.input-container {
  display: flex;
  gap: 0.5rem;
}

textarea {
  flex: 1;
  padding: 0.75rem;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  resize: none;
  height: 40px;
  font-family: inherit;
}

.send-button {
  width: 40px;
  height: 40px;
  border: none;
  border-radius: 4px;
  background-color: #4285f4;
  color: white;
  cursor: pointer;
  transition: background-color 0.2s;
}

.send-button:hover {
  background-color: #3367d6;
}

.send-button:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

.connection-status {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 0.5rem;
  font-size: 0.875rem;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.connected .status-indicator {
  background-color: #4caf50;
}

.disconnected .status-indicator {
  background-color: #f44336;
}

.modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background-color: #fff;
  border-radius: 8px;
  width: 400px;
  max-width: 90%;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border-bottom: 1px solid #e0e0e0;
}

.close-btn {
  background: none;
  border: none;
  cursor: pointer;
  color: #666;
}

.modal-body {
  padding: 1rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #666;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  font-family: inherit;
}

.modal-footer {
  padding: 1rem;
  border-top: 1px solid #e0e0e0;
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}

.cancel-btn,
.confirm-btn {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.cancel-btn {
  background-color: #f5f5f5;
  color: #666;
}

.confirm-btn {
  background-color: #4285f4;
  color: white;
}

.empty-state,
.empty-groups {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  color: #666;
  text-align: center;
}

.empty-state .material-icon,
.empty-groups .material-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
  color: #ccc;
}
</style> 