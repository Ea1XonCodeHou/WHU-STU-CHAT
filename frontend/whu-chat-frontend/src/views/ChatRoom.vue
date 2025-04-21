<template>
  <div class="chatroom-container">
    <!-- 聊天室顶部区域 -->
    <div class="chatroom-header">
      <div class="room-info">
        <h1 class="room-name">{{ roomName }}</h1>
        <div class="user-count">
          <span class="material-icon">group</span>
          <span>{{ onlineUsers.length }} 人在线</span>
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
        <button @click="logout" class="logout-btn">
          <span class="material-icon">logout</span>
          <span>退出</span>
        </button>
      </div>
    </div>

    <!-- 聊天内容和用户列表区域 -->
    <div class="chatroom-content">
      <!-- 聊天消息区域 -->
      <div class="chat-messages" ref="messagesContainer">
        <div class="messages-list">
          <div v-if="messages.length === 0" class="empty-state">
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
                  <div class="message-time">{{ formatTime(message.sendTime) }}</div>
                </div>
              </div>
              
              <div class="message-content">
                {{ message.content }}
              </div>
              
              <!-- 自己的消息右侧显示时间 -->
              <div v-if="message.senderId === userId" class="my-message-time">
                {{ formatTime(message.sendTime) }}
              </div>
            </template>
          </div>
        </div>
      </div>

      <!-- 在线用户列表 -->
      <div class="online-users-panel">
        <div class="panel-header">
          <h3>在线用户</h3>
        </div>
        <div class="users-list">
          <div 
            v-for="user in onlineUsers" 
            :key="user.id" 
            class="user-item"
            :class="{ 'current-user': user.id === userId }"
          >
            <div class="user-avatar">
              <img v-if="user.avatarUrl" :src="user.avatarUrl" :alt="user.username">
              <span v-else class="default-avatar">{{ user.username.charAt(0).toUpperCase() }}</span>
            </div>
            <div class="user-details">
              <span class="user-name">{{ user.username }}</span>
              <span class="user-status" :class="user.status">{{ user.status === 'online' ? '在线' : '离线' }}</span>
            </div>
          </div>
          
          <div v-if="onlineUsers.length === 0" class="empty-users">
            <span class="material-icon">people</span>
            <p>暂无在线用户</p>
          </div>
        </div>
      </div>
    </div>

    <!-- 聊天输入区域 -->
    <div class="chat-input-area">
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
  </div>
</template>

<script>
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export default {
  name: 'ChatRoomView',
  data() {
    return {
      connection: null,
      roomId: 1, // 默认聊天室ID
      roomName: 'WHU 校园公共聊天室',
      messages: [],
      message: '',
      onlineUsers: [],
      isConnected: false,
      isLoading: true,
      error: null,
      userId: null,
      username: '',
      userAvatar: null
    }
  },
  computed: {
    currentUserInfo() {
      return this.onlineUsers.find(user => user.id === this.userId) || null;
    }
  },
  async created() {
    // 从localStorage获取用户信息
    this.userId = localStorage.getItem('userId');
    this.username = localStorage.getItem('userUsername');
    this.userAvatar = localStorage.getItem('userAvatar');
    
    // 检查是否已登录
    const token = localStorage.getItem('token');
    if (!token) {
      // 未登录，重定向到登录页
      this.$router.push('/login');
      return;
    }
    
    // 初始化SignalR连接
    await this.initSignalRConnection();
    
    // 设置页面标题
    document.title = `${this.roomName} | WHU-Chat`;
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
    // 初始化SignalR连接
    async initSignalRConnection() {
      try {
        // 创建连接
        this.connection = new HubConnectionBuilder()
          .withUrl('http://localhost:5067/chatHub')
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build();
        
        // 监听接收消息
        this.connection.on('ReceiveMessage', (message) => {
          this.messages.push(message);
          this.$nextTick(this.scrollToBottom);
        });
        
        // 监听接收历史消息
        this.connection.on('ReceiveHistoryMessages', (messages) => {
          this.messages = messages;
          this.$nextTick(this.scrollToBottom);
        });
        
        // 监听在线用户列表更新
        this.connection.on('UpdateOnlineUsers', (users) => {
          this.onlineUsers = users;
        });
        
        // 监听错误消息
        this.connection.on('Error', (errorMessage) => {
          console.error('SignalR错误:', errorMessage);
          // 可以显示错误提示
        });
        
        // 启动连接
        await this.connection.start();
        console.log('SignalR连接已建立');
        this.isConnected = true;
        
        // 加入聊天室
        const userIdInt = this.userId ? parseInt(this.userId) : 0;
        await this.connection.invoke('JoinChatRoom', userIdInt, this.username, this.roomId);
        console.log('已加入聊天室');
        
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
      if (!this.isConnected || !this.message.trim()) return;
      
      try {
        // 发送消息到SignalR Hub
        await this.connection.invoke('SendMessageToRoom', this.message.trim());
        // 清空消息输入框
        this.message = '';
        // 聚焦输入框
        this.$refs.messageInput.focus();
      } catch (error) {
        console.error('发送消息失败:', error);
      }
    },
    
    // 滚动到底部
    scrollToBottom() {
      if (this.$refs.messagesContainer) {
        const container = this.$refs.messagesContainer;
        container.scrollTop = container.scrollHeight;
      }
    },
    
    // 格式化时间
    formatTime(timestamp) {
      if (!timestamp) return '';
      
      const date = new Date(timestamp);
      const now = new Date();
      const diff = now - date;
      
      // 如果是今天的消息，只显示时分
      if (date.toDateString() === now.toDateString()) {
        return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
      }
      
      // 如果是昨天的消息
      if (diff < 24 * 60 * 60 * 1000 && date.toDateString() === new Date(now - 24 * 60 * 60 * 1000).toDateString()) {
        return `昨天 ${date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })}`;
      }
      
      // 其他情况显示年月日时分
      return date.toLocaleString('zh-CN', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit',
        hour: '2-digit', 
        minute: '2-digit'
      });
    },
    
    // 登出
    logout() {
      // 清除localStorage中的用户信息
      localStorage.removeItem('token');
      localStorage.removeItem('userId');
      localStorage.removeItem('userUsername');
      localStorage.removeItem('userAvatar');
      localStorage.removeItem('lastLoginTime');
      
      // 刷新页面或重定向到登录页
      this.$router.push('/login');
    }
  }
}
</script>

<style scoped>
.chatroom-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fb;
  color: #333;
}

.chatroom-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background-color: #fff;
  box-shadow: 0 2px 5px rgba(0,0,0,0.1);
  z-index: 10;
}

.room-info {
  display: flex;
  flex-direction: column;
}

.room-name {
  font-size: 1.5rem;
  font-weight: 600;
  margin: 0;
  color: #2c3e50;
}

.user-count {
  display: flex;
  align-items: center;
  font-size: 0.9rem;
  color: #7d8a96;
  margin-top: 4px;
}

.user-count .material-icon {
  font-size: 1rem;
  margin-right: 4px;
}

.user-actions {
  display: flex;
  align-items: center;
}

.current-user {
  display: flex;
  align-items: center;
  margin-right: 15px;
}

.user-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background-color: #e1e5ea;
  margin-right: 8px;
  overflow: hidden;
}

.user-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.default-avatar {
  font-weight: bold;
  color: #fff;
  background-color: #4285f4;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.username {
  font-weight: 500;
}

.logout-btn {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  border-radius: 4px;
  background: none;
  border: 1px solid #e1e5ea;
  cursor: pointer;
  color: #5c6973;
  text-decoration: none;
  transition: all 0.2s ease;
}

.logout-btn:hover {
  background-color: #f0f2f5;
}

.logout-btn .material-icon {
  margin-right: 4px;
  font-size: 1rem;
}

.chatroom-content {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.chat-messages {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
  background-color: #fff;
}

.messages-list {
  display: flex;
  flex-direction: column;
}

.message-item {
  display: flex;
  flex-direction: column;
  margin-bottom: 16px;
  max-width: 80%;
}

.my-message {
  align-self: flex-end;
}

.system-message {
  align-self: center;
  max-width: 90%;
}

.message-sender {
  display: flex;
  align-items: center;
  margin-bottom: 4px;
}

.sender-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  margin-right: 8px;
  overflow: hidden;
}

.message-info {
  display: flex;
  flex-direction: column;
}

.sender-name {
  font-weight: 500;
  font-size: 0.9rem;
}

.message-time, .my-message-time {
  font-size: 0.8rem;
  color: #a0a8b1;
}

.message-content {
  padding: 12px 16px;
  border-radius: 18px;
  background-color: #f0f2f5;
  color: #333;
  word-break: break-word;
}

.my-message .message-content {
  background-color: #4285f4;
  color: white;
  border-top-right-radius: 4px;
}

.system-message-content {
  padding: 8px 12px;
  border-radius: 16px;
  background-color: rgba(0, 0, 0, 0.05);
  font-size: 0.9rem;
  color: #666;
  text-align: center;
}

.my-message-time {
  align-self: flex-end;
  margin-top: 4px;
}

.online-users-panel {
  width: 250px;
  border-left: 1px solid #e1e5ea;
  background-color: #fff;
  display: flex;
  flex-direction: column;
}

.panel-header {
  padding: 15px;
  border-bottom: 1px solid #e1e5ea;
}

.panel-header h3 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 600;
  color: #2c3e50;
}

.users-list {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
}

.user-item {
  display: flex;
  align-items: center;
  padding: 8px 0;
  cursor: pointer;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.user-item:hover {
  background-color: #f5f7fb;
}

.user-item.current-user {
  background-color: rgba(66, 133, 244, 0.1);
}

.user-details {
  display: flex;
  flex-direction: column;
  margin-left: 10px;
}

.user-name {
  font-weight: 500;
  font-size: 0.95rem;
}

.user-status {
  font-size: 0.75rem;
  margin-top: 2px;
}

.user-status.online {
  color: #34c759;
}

.user-status.offline {
  color: #a0a8b1;
}

.empty-state, .empty-users {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  color: #a0a8b1;
  text-align: center;
}

.empty-state .material-icon, .empty-users .material-icon {
  font-size: 3rem;
  margin-bottom: 12px;
  opacity: 0.5;
}

.chat-input-area {
  padding: 15px 20px;
  border-top: 1px solid #e1e5ea;
  background-color: #fff;
}

.input-container {
  display: flex;
  align-items: flex-end;
  background-color: #f5f7fb;
  border-radius: 20px;
  padding: 10px 15px;
  transition: box-shadow 0.2s ease;
}

.input-container:focus-within {
  box-shadow: 0 0 0 2px rgba(66, 133, 244, 0.2);
}

.input-container textarea {
  flex: 1;
  border: none;
  background: none;
  min-height: 24px;
  max-height: 150px;
  padding: 0;
  resize: none;
  outline: none;
  font-family: inherit;
  font-size: 0.95rem;
  line-height: 1.5;
}

.send-button {
  background-color: #4285f4;
  color: white;
  border: none;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-left: 8px;
  transition: background-color 0.2s;
}

.send-button:hover {
  background-color: #3367d6;
}

.send-button:disabled {
  background-color: #a0a8b1;
  cursor: not-allowed;
}

.connection-status {
  display: flex;
  align-items: center;
  font-size: 0.8rem;
  color: #a0a8b1;
  margin-top: 8px;
  padding-left: 15px;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-right: 6px;
}

.connected .status-indicator {
  background-color: #34c759;
}

.disconnected .status-indicator {
  background-color: #ff3b30;
}

.material-icon {
  font-family: 'Material Symbols Outlined';
  font-weight: normal;
  font-style: normal;
  line-height: 1;
  letter-spacing: normal;
  text-transform: none;
  display: inline-block;
  white-space: nowrap;
  word-wrap: normal;
  direction: ltr;
  -webkit-font-feature-settings: 'liga';
  -webkit-font-smoothing: antialiased;
}
</style> 