<template>
  <div class="private-chat-container">
    <div class="header">
      <div class="chat-info">
        <div class="avatar-container">
          <img v-if="friendInfo.avatar" :src="friendInfo.avatar" class="avatar" :alt="friendInfo.username" />
          <div v-else class="avatar">{{ friendInfo.username ? friendInfo.username.charAt(0).toUpperCase() : 'U' }}</div>
          <div class="status-indicator" :class="friendInfo.status || 'offline'"></div>
        </div>
        <div class="user-details">
          <div class="username">{{ friendInfo.username || '未知用户' }}</div>
          <div class="status-text">{{ friendInfo.status === 'online' ? '在线' : '离线' }}</div>
          <div v-if="friendInfo.signature" class="signature">{{ friendInfo.signature }}</div>
        </div>
      </div>
      <div class="header-actions">
        <button class="action-button" @click="goBack">
          <i class="fas fa-arrow-left"></i>
        </button>
      </div>
    </div>
    
    <div class="chat-content">
      <div class="empty-chat-message" v-if="messages.length === 0">
        <i class="fas fa-comments"></i>
        <p>还没有消息，开始聊天吧！</p>
      </div>
    </div>
    
    <div class="chat-input">
      <textarea 
        v-model="newMessage" 
        placeholder="请输入消息..." 
        @keyup.enter="sendMessage"
      ></textarea>
      <button @click="sendMessage" :disabled="!newMessage.trim()">
        <i class="fas fa-paper-plane"></i>
      </button>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import axios from 'axios';
import * as signalR from '@microsoft/signalr';

export default {
  name: 'PrivateChat',
  setup() {
    const route = useRoute();
    const router = useRouter();
    const friendId = route.params.id;
    const userId = ref(localStorage.getItem('userId') || '0');
    const friendInfo = ref({
      username: '',
      avatar: '',
      status: 'offline',
      signature: ''
    });
    const messages = ref([]);
    const newMessage = ref('');
    const connectionStatus = ref('disconnected'); // 'disconnected', 'connecting', 'connected'
    const connection = ref(null);
    
    // 建立SignalR连接
    const setupSignalRConnection = async () => {
      try {
        connectionStatus.value = 'connecting';
        
        // 创建连接
        connection.value = new signalR.HubConnectionBuilder()
          .withUrl('/privateChatHub?userId=' + userId.value)
          .withAutomaticReconnect()
          .build();
        
        // 监听接收私聊消息
        connection.value.on('ReceivePrivateMessage', (message) => {
          console.log('收到私聊消息:', message);
          messages.value.push(message);
          setTimeout(() => {
            scrollToBottom();
          }, 50);
        });
        
        // 启动连接
        await connection.value.start();
        connectionStatus.value = 'connected';
        console.log('SignalR连接已建立');
        
        // 加入私聊组
        if (friendId) {
          await connection.value.invoke('JoinPrivateChat', friendId);
          console.log(`已加入与用户${friendId}的私聊组`);
        }
      } catch (err) {
        console.error('建立SignalR连接失败:', err);
        connectionStatus.value = 'disconnected';
      }
    };
    
    // 滚动到底部
    const scrollToBottom = () => {
      const chatContent = document.querySelector('.chat-content');
      if (chatContent) {
        chatContent.scrollTop = chatContent.scrollHeight;
      }
    };
    
    // 加载历史消息
    const loadChatHistory = async () => {
      try {
        if (!friendId) return;
        
        const response = await axios.get(`/api/chat/history/private/${friendId}`);
        if (response.data && Array.isArray(response.data)) {
          messages.value = response.data;
          setTimeout(() => {
            scrollToBottom();
          }, 50);
        }
      } catch (error) {
        console.error('加载历史消息失败:', error);
      }
    };
    
    // 获取好友信息
    const fetchFriendInfo = async () => {
      try {
        if (!friendId) return;
        
        const response = await axios.get(`/api/user/${friendId}`);
        if (response.data) {
          friendInfo.value = {
            ...friendInfo.value,
            ...response.data,
            avatar: formatAvatarUrl(response.data.avatar)
          };
        }
      } catch (error) {
        console.error('获取好友信息失败:', error);
      }
    };
    
    // 处理头像URL
    const formatAvatarUrl = (avatarPath) => {
      if (!avatarPath) return '';
      if (avatarPath.startsWith('http')) return avatarPath;
      
      const origin = window.location.origin;
      return avatarPath.startsWith('/') ? `${origin}${avatarPath}` : `${origin}/${avatarPath}`;
    };
    
    // 发送消息
    const sendMessage = async () => {
      if (!newMessage.value.trim() || connectionStatus.value !== 'connected') return;
      
      try {
        if (!friendId) return;
        
        // 构建消息对象
        const message = {
          senderId: parseInt(userId.value),
          receiverId: parseInt(friendId),
          content: newMessage.value.trim(),
          sendTime: new Date().toISOString()
        };
        
        // 调用SignalR方法发送消息
        await connection.value.invoke('SendPrivateMessage', message);
        console.log('消息已发送:', message);
        
        // 清空输入框
        newMessage.value = '';
      } catch (error) {
        console.error('发送消息失败:', error);
        alert('发送消息失败，请重试');
      }
    };
    
    // 返回主页
    const goBack = () => {
      router.push('/home');
    };
    
    // 组件挂载时
    onMounted(async () => {
      await fetchFriendInfo();
      await setupSignalRConnection();
      await loadChatHistory();
    });
    
    // 组件卸载时断开连接
    onUnmounted(async () => {
      if (connection.value) {
        try {
          await connection.value.stop();
          console.log('SignalR连接已断开');
        } catch (err) {
          console.error('断开SignalR连接失败:', err);
        }
      }
    });
    
    return {
      friendInfo,
      messages,
      newMessage,
      connectionStatus,
      sendMessage,
      goBack,
      formatAvatarUrl
    };
  }
};
</script>

<style scoped>
.private-chat-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: var(--background-color, #ffffff);
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.chat-info {
  display: flex;
  align-items: center;
}

.avatar-container {
  position: relative;
  margin-right: 15px;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: #8E54E9;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: bold;
  overflow: hidden;
}

.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.status-indicator {
  position: absolute;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  border: 2px solid #4776E6;
  bottom: 0;
  right: 0;
}

.status-indicator.online {
  background-color: #4CD964;
}

.status-indicator.offline {
  background-color: #8A8A8E;
}

.user-details {
  display: flex;
  flex-direction: column;
}

.username {
  font-weight: 600;
  font-size: 16px;
}

.status-text {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.8);
}

.signature {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.8);
  margin-top: 2px;
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.header-actions {
  display: flex;
}

.action-button {
  background: none;
  border: none;
  color: white;
  font-size: 16px;
  cursor: pointer;
  padding: 8px;
  transition: all 0.2s;
}

.action-button:hover {
  transform: scale(1.1);
}

.chat-content {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
}

.empty-chat-message {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #999;
  text-align: center;
}

.empty-chat-message i {
  font-size: 40px;
  margin-bottom: 10px;
  color: #ddd;
}

.chat-input {
  display: flex;
  padding: 15px;
  background-color: #f8f8f8;
  border-top: 1px solid #eaeaea;
  position: relative;
}

.chat-input textarea {
  flex: 1;
  border: 1px solid #ddd;
  border-radius: 20px;
  padding: 10px 15px;
  resize: none;
  outline: none;
  font-size: 14px;
  line-height: 1.5;
  max-height: 100px;
  overflow-y: auto;
}

.chat-input button {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-left: 10px;
  cursor: pointer;
  transition: all 0.2s;
}

.chat-input button:hover {
  transform: scale(1.1);
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.chat-input button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}
</style>
