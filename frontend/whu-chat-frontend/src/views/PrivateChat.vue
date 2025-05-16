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
    
    <div class="chat-content" ref="chatContent">
      <div class="empty-chat-message" v-if="messages.length === 0">
        <i class="fas fa-comments"></i>
        <p>还没有消息，开始聊天吧！</p>
      </div>
      
      <div v-else class="messages-wrapper">
        <div v-for="(message, index) in messages" 
             :key="message.messageId || index" 
             class="message-item"
             :class="{'self-message': message.senderId === userId}">
          
          <div v-if="shouldShowDateSeparator(index)" class="date-separator">
            <span>{{ formatDate(message.sendTime) }}</span>
          </div>
          
          <div class="message-content">
            <div class="message-avatar" v-if="message.senderId !== userId">
              <div class="avatar default-avatar">
                {{ message.senderName?.charAt(0)?.toUpperCase() || '?' }}
              </div>
            </div>
            
            <div class="message-body">
              <div class="message-info">
                <span class="message-sender" v-if="message.senderId !== userId">
                  {{ message.senderName }}
                </span>
                <span class="message-time">{{ formatTime(message.sendTime) }}</span>
              </div>
              
              <div v-if="message.messageType === 'text'" class="message-text">
                {{ message.content }}
              </div>
              
              <div v-else-if="message.messageType === 'image'" class="message-image">
                <img :src="message.fileUrl" alt="图片消息" @click="previewImage(message.fileUrl)" />
                <div class="image-info">{{ message.fileName }} ({{ formatFileSize(message.fileSize) }})</div>
              </div>
              
              <div v-else-if="message.messageType === 'file'" class="message-file" @click="downloadFile(message.fileUrl, message.fileName)">
                <div class="file-icon"></div>
                <div class="file-info">
                  <div class="file-name">{{ message.fileName }}</div>
                  <div class="file-size">{{ formatFileSize(message.fileSize) }}</div>
                </div>
                <div class="download-icon"></div>
              </div>
              
              <div v-else-if="message.messageType === 'emoji'" class="message-emoji">
                {{ message.content }}
              </div>
            </div>
            
            <div class="message-avatar self-avatar" v-if="message.senderId === userId">
              <div class="avatar default-avatar">
                {{ username.charAt(0).toUpperCase() }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <div class="chat-input">
      <div class="toolbar">
        <div class="tool-button emoji-button" @click="toggleEmojiPanel">
          <i class="fas fa-smile"></i>
        </div>
        <div class="tool-button file-button">
          <input
            type="file"
            ref="fileInput"
            @change="handleFileUpload"
            style="display: none"
          />
          <i class="fas fa-paperclip" @click="triggerFileInput"></i>
        </div>
      </div>
      
      <div v-show="showEmojiPanel" class="emoji-panel">
        <div v-for="emoji in emojis" 
             :key="emoji" 
             class="emoji-item" 
             @click="insertEmoji(emoji)">
          {{ emoji }}
        </div>
      </div>
      
      <textarea 
        v-model="newMessage" 
        placeholder="请输入消息..." 
        @keyup.enter.exact.prevent="sendMessage"
        @keydown.ctrl.enter="addNewLine"
        ref="messageInput"></textarea>
        
      <button @click="sendMessage" :disabled="!newMessage.trim() || !isConnected">
        <i class="fas fa-paper-plane"></i>
      </button>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, onUnmounted, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import axios from 'axios';
import * as signalR from '@microsoft/signalr';

export default {
  name: 'PrivateChat',
  setup() {
    const route = useRoute();
    const router = useRouter();
    const friendId = route.params.id;
    const userId = ref(parseInt(localStorage.getItem('userId') || '0'));
    const username = ref(localStorage.getItem('username') || '访客');
    const friendInfo = ref({
      username: '',
      avatar: '',
      status: 'offline',
      signature: ''
    });
    
    const messages = ref([]);
    const newMessage = ref('');
    const chatContent = ref(null);
    const messageInput = ref(null);
    const isConnected = ref(false);
    const connection = ref(null);
    
    const showEmojiPanel = ref(false);
    const emojis = ref(['😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣', '🥲', '☺️', '😊', '😇', 
                      '🙂', '🙃', '😉', '😌', '😍', '🥰', '😘', '😗', '😙', '😚', '😋', '😛',
                      '😝', '😜', '🤪', '🤨', '🧐', '🤓', '😎', '🤩', '🥳', '😏', '😒', '😞',
                      '😔', '😟', '😕', '🙁', '☹️', '😣', '😖', '😫', '😩', '🥺', '😢', '😭',
                      '😤', '😠', '😡', '🤬', '🤯', '😳', '🥵', '🥶', '😱', '😨', '😰', '😥',
                      '😓', '🤗', '🤔', '🤭', '🤫', '🤥', '😶', '😐', '😑', '😬', '🙄', '😯',
                      '😦', '😧', '😮', '😲', '🥱', '😴', '🤤', '😪', '😵', '🤐', '🥴', '🤢',
                      '🤮', '🤧', '😷', '🤒', '🤕', '🤑', '🤠', '💩', '👻', '👽', '👾', '🤖',
                      '😺', '😸', '😹', '😻', '😼', '😽', '🙀', '😿', '😾', '🙈', '🙉', '🙊',
                      '👶', '👧', '🧒', '👦', '👩', '🧑', '👨', '👵', '🧓', '👴', '👮', '🕵️',
                      '👷', '👸', '🤴', '👳', '👲', '🧕', '🤵', '👰', '🤰', '🤱', '👼', '🎅',
                      '🤶', '🦸', '🦹', '🧙', '🧚', '🧛', '🧜', '🧝', '🧞', '🧟', '🧌', '💆',
                      '💇', '🚶', '🧍', '🧎', '🏃', '💃', '🕺', '🕴️', '👯', '🧖', '🧗', '🤺',
                      '🏇', '⛷️', '🏂', '🏌️', '🏄', '🚣', '🏊', '⛹️', '🏋️', '🚴', '🚵', '🤸',
                      '🤼', '🤽', '🤾', '🤹', '🧘', '🛀', '🛌', '👭', '👫', '👬', '💏', '💑',
                      '🗣️', '👤', '👥', '👣', '🦰', '🦱', '🦳', '🦲', '🧠', '🫀', '🫁', '🦷',
                      '🦴', '👀', '👁️', '👅', '👄', '💋', '🩸', '🩹', '🩺', '💊', '💉', '🦠',
                      '🧬', '🦡', '🦫', '🦦', '🦨', '🦘', '🦥', '🦙', '🦒', '🦣', '🦏', '🦛',
                      '🦍', '🦧', '🐶', '🐕', '🦮', '🐩', '🐺', '🦊', '🦝', '🐱', '🐈', '🐈‍⬛',
                      '🐆', '🐅', '🐃', '🐂', '🐄', '🦬', '🐪', '🐫', '🦙', '🦒', '🐘', '🦣',
                      '🦏', '🦛', '🦍', '🦧', '🐒', '🦍', '🦧', '🐶', '🐕', '🦮', '🐩', '🐺',
                      '🦊', '🦝', '🐱', '🐈', '🐈‍⬛', '🐆', '🐅', '🐃', '🐂', '🐄', '🦬', '🐪',
                      '🐫', '🦙', '🦒', '🐘', '🦣', '🦏', '🦛', '🦍', '🦧', '🐒', '🦍', '🦧']);
    
    const fileInput = ref(null);
    
    const loadChatHistory = async () => {
      try {
        if (!friendId) return;
        
        // 使用SignalR获取历史消息
        if (connection.value && isConnected.value) {
          await connection.value.invoke('GetPrivateChatHistory', parseInt(friendId), 50);
        } else {
          console.warn('SignalR连接未建立，无法获取历史消息');
          showNotification('连接未建立，请稍后重试', 'warning');
        }
      } catch (error) {
        console.error('获取历史消息失败:', error);
        showNotification('获取历史消息失败', 'error');
      }
    };
    
    const loadFriendInfo = async () => {
      try {
        if (!friendId) return;
        
        const response = await axios.get(
          `${window.apiBaseUrl}/api/user/${friendId}`,
          {
            headers: {
              'UserId': userId.value.toString()
            }
          }
        );
        
        if (response.data && response.data.code === 200 && response.data.data) {
          friendInfo.value = {
            username: response.data.data.username,
            avatar: response.data.data.avatar,
            status: response.data.data.status || 'offline',
            signature: response.data.data.signature
          };
        } else {
          console.error('获取好友信息失败:', response.data?.msg || '未知错误');
          showNotification('获取好友信息失败', 'error');
        }
      } catch (error) {
        console.error('加载好友信息失败:', error);
        showNotification('加载好友信息失败', 'error');
      }
    };
    
    const setupSignalR = async () => {
      try {
        connection.value = new signalR.HubConnectionBuilder()
          .withUrl(`${window.apiBaseUrl}/privateChatHub?userId=${userId.value}`)
          .withAutomaticReconnect()
          .build();
        
        // 注册连接
        connection.value.on('ReceivePrivateMessage', (message) => {
          messages.value.push(message);
          nextTick(() => scrollToBottom());
        });
        
        // 接收历史消息
        connection.value.on('ReceiveHistoryMessages', (historyMessages) => {
          console.log('收到历史消息:', historyMessages);
          if (Array.isArray(historyMessages)) {
            messages.value = historyMessages;
            nextTick(() => scrollToBottom());
          } else {
            console.warn('历史消息数据格式不正确:', historyMessages);
          }
        });
        
        // 监听用户状态变化
        connection.value.on('UserStatusChanged', (changedUserId, status) => {
          console.log('用户状态变化:', changedUserId, status);
          if (changedUserId === parseInt(friendId)) {
            friendInfo.value.status = status;
          }
        });
        
        // 错误处理
        connection.value.on('Error', (error) => {
          console.error('SignalR错误:', error);
          showNotification(error, 'error');
        });
        
        await connection.value.start();
        isConnected.value = true;
        
        // 注册连接
        await connection.value.invoke('RegisterConnection', userId.value);
        
        // 加入私聊
        await connection.value.invoke('JoinPrivateChat', parseInt(friendId));
        
        // 获取历史消息
        await loadChatHistory();
        
      } catch (error) {
        console.error('SignalR连接失败:', error);
        showNotification('连接失败，请刷新页面重试', 'error');
      }
    };
    
    const insertEmoji = (emoji) => {
      newMessage.value += emoji;
      showEmojiPanel.value = false;
      messageInput.value?.focus();
    };
    
    const toggleEmojiPanel = () => {
      showEmojiPanel.value = !showEmojiPanel.value;
      if (showEmojiPanel.value) {
        // 确保表情面板显示在正确的位置
        nextTick(() => {
          const emojiPanel = document.querySelector('.emoji-panel');
          if (emojiPanel) {
            const inputRect = document.querySelector('.chat-input').getBoundingClientRect();
            emojiPanel.style.bottom = `${inputRect.height}px`;
          }
        });
      }
    };
    
    // 点击其他地方时关闭表情面板
    const handleClickOutside = (event) => {
      const emojiButton = document.querySelector('.emoji-button');
      const emojiPanel = document.querySelector('.emoji-panel');
      
      if (emojiPanel && 
          !emojiPanel.contains(event.target) && 
          !emojiButton.contains(event.target)) {
        showEmojiPanel.value = false;
      }
    };
    
    const addNewLine = () => {
      newMessage.value += '\n';
    };
    
    const scrollToBottom = () => {
      if (chatContent.value) {
        chatContent.value.scrollTop = chatContent.value.scrollHeight;
      }
    };
    
    const formatDate = (dateString) => {
      const date = new Date(dateString);
      const today = new Date();
      const yesterday = new Date(today);
      yesterday.setDate(yesterday.getDate() - 1);
      
      if (date.toDateString() === today.toDateString()) {
        return '今天';
      } else if (date.toDateString() === yesterday.toDateString()) {
        return '昨天';
      } else {
        return date.toLocaleDateString('zh-CN', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit'
        });
      }
    };
    
    const formatTime = (dateString) => {
      const date = new Date(dateString);
      return date.toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
      });
    };
    
    const formatFileSize = (bytes) => {
      if (!bytes) return '未知大小';
      const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
      if (bytes === 0) return '0 B';
      const i = Math.floor(Math.log(bytes) / Math.log(1024));
      return (bytes / Math.pow(1024, i)).toFixed(2) + ' ' + sizes[i];
    };
    
    const shouldShowDateSeparator = (index) => {
      if (index === 0) return true;
      
      const currentDate = new Date(messages.value[index].sendTime).setHours(0, 0, 0, 0);
      const prevDate = new Date(messages.value[index - 1].sendTime).setHours(0, 0, 0, 0);
      
      return currentDate !== prevDate;
    };
    
    const showNotification = (message, type = 'info') => {
      console.log(`${type}: ${message}`);
    };
    
    const goBack = () => {
      router.push('/home');
    };
    
    const triggerFileInput = () => {
      fileInput.value.click();
    };
    
    const handleFileUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        showNotification('文件上传中...', 'info');
        
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        await connection.value.invoke(
          'SendFileToPrivate', 
          parseInt(friendId),
          response.data.url, 
          response.data.fileName, 
          response.data.fileSize
        );
        
        fileInput.value.value = '';
        showNotification('文件发送成功', 'success');
      } catch (error) {
        console.error('文件上传失败:', error);
        showNotification('文件上传失败: ' + (error.response?.data || error.message), 'error');
        fileInput.value.value = '';
      }
    };
    
    const downloadFile = async (fileUrl, fileName) => {
      try {
        const response = await axios.get(fileUrl, {
          responseType: 'blob'
        });
        
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', fileName);
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      } catch (error) {
        console.error('文件下载失败:', error);
        showNotification('文件下载失败', 'error');
      }
    };
    
    const sendMessage = async () => {
      if (!newMessage.value.trim() || !isConnected.value) return;
      
      try {
        const message = {
          senderId: userId.value,
          senderName: username.value,
          receiverId: parseInt(friendId),
          content: newMessage.value.trim(),
          messageType: 'text',
          sendTime: new Date().toISOString()
        };
        
        // 检查是否是表情消息
        if (emojis.value.includes(newMessage.value.trim())) {
          message.messageType = 'emoji';
        }
        
        await connection.value.invoke('SendPrivateMessage', message);
        
        newMessage.value = '';
        
        messageInput.value?.focus();
      } catch (error) {
        console.error('发送消息失败:', error);
        showNotification('发送消息失败，请重试', 'error');
      }
    };
    
    onMounted(async () => {
      await loadFriendInfo();
      await setupSignalR();
      document.addEventListener('click', handleClickOutside);
    });
    
    onUnmounted(async () => {
      if (connection.value) {
        try {
          await connection.value.stop();
          console.log('SignalR连接已断开');
        } catch (err) {
          console.error('断开SignalR连接失败:', err);
        }
      }
      document.removeEventListener('click', handleClickOutside);
    });
    
    return {
      userId,
      username,
      friendInfo,
      messages,
      newMessage,
      chatContent,
      messageInput,
      isConnected,
      showEmojiPanel,
      emojis,
      sendMessage,
      insertEmoji,
      toggleEmojiPanel,
      addNewLine,
      formatDate,
      formatTime,
      formatFileSize,
      shouldShowDateSeparator,
      goBack,
      fileInput,
      triggerFileInput,
      handleFileUpload,
      downloadFile
    };
  }
};
</script>

<style scoped>
.private-chat-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f5f5;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
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
  margin-right: 12px;
}

.avatar {
  width: 36px;
  height: 36px;
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
  width: 8px;
  height: 8px;
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
  font-size: 15px;
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
  background-color: #f5f5f5;
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

.messages-wrapper {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
}

.message-item {
  display: flex;
  flex-direction: column;
  max-width: 80%;
}

.self-message {
  align-self: flex-end;
}

.other-message {
  align-self: flex-start;
}

.message-content {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}

.self-message .message-content {
  flex-direction: row-reverse;
}

.message-avatar {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
}

.avatar.default-avatar {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background-color: #8E54E9;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: bold;
  font-size: 14px;
}

.message-body {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.message-info {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.message-sender {
  font-size: 12px;
  color: #666;
  font-weight: 500;
}

.message-time {
  font-size: 11px;
  color: #999;
}

.self-message .message-info {
  align-items: flex-end;
}

.other-message .message-info {
  align-items: flex-start;
}

.message-text {
  padding: 10px 14px;
  border-radius: 12px;
  font-size: 14px;
  line-height: 1.5;
  word-break: break-word;
  max-width: 100%;
}

.self-message .message-text {
  background-color: #4776E6;
  color: white;
  border-top-right-radius: 4px;
}

.other-message .message-text {
  background-color: white;
  color: #333;
  border-top-left-radius: 4px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.date-separator {
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 16px 0;
  color: #999;
  font-size: 12px;
  width: 100%;
  position: relative;
}

.date-separator span {
  padding: 4px 12px;
  background-color: rgba(0, 0, 0, 0.05);
  border-radius: 12px;
  text-align: center;
  position: relative;
  z-index: 1;
}

.chat-input {
  display: flex;
  padding: 12px;
  background-color: white;
  border-top: 1px solid #eee;
  gap: 8px;
}

.toolbar {
  display: flex;
  align-items: center;
}

.tool-button {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: #666;
  transition: all 0.2s;
  position: relative;
}

.tool-button:hover {
  background-color: #f5f5f5;
  color: #4776E6;
}

.file-button {
  margin-left: 8px;
}

.emoji-panel {
  position: absolute;
  left: 12px;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  padding: 8px;
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 4px;
  max-height: 300px;
  overflow-y: auto;
  z-index: 1000;
  width: 320px;
}

.emoji-item {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  border-radius: 4px;
  transition: all 0.2s;
  font-size: 20px;
}

.emoji-item:hover {
  background-color: #f5f5f5;
  transform: scale(1.1);
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
  transition: all 0.2s;
}

.chat-input textarea:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
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
  cursor: pointer;
  transition: all 0.2s;
}

.chat-input button:hover {
  transform: scale(1.05);
  box-shadow: 0 2px 8px rgba(71, 118, 230, 0.3);
}

.chat-input button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

/* 图片消息样式 */
.message-image {
  max-width: 300px;
  border-radius: 12px;
  overflow: hidden;
}

.message-image img {
  width: 100%;
  height: auto;
  cursor: pointer;
  transition: all 0.2s;
}

.message-image img:hover {
  transform: scale(1.02);
}

.image-info {
  font-size: 12px;
  color: #999;
  margin-top: 4px;
}

/* 文件消息样式 */
.message-file {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background-color: #f8f9fa;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.message-file:hover {
  background-color: #e9ecef;
}

.file-icon {
  width: 24px;
  height: 24px;
  background-color: #4776E6;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
}

.file-info {
  flex: 1;
  min-width: 0;
}

.file-name {
  font-size: 13px;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.file-size {
  font-size: 11px;
  color: #999;
}

.download-icon {
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #666;
}

/* 图片预览弹窗 */
.image-preview-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.9);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.image-preview-content {
  position: relative;
  max-width: 90%;
  max-height: 90%;
}

.image-preview-content img {
  max-width: 100%;
  max-height: 90vh;
  object-fit: contain;
}

.close-preview {
  position: absolute;
  top: -40px;
  right: 0;
  background: none;
  border: none;
  color: white;
  font-size: 24px;
  cursor: pointer;
  padding: 8px;
}

/* 通知提示 */
.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 12px 20px;
  border-radius: 8px;
  background-color: white;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  z-index: 1000;
  animation: slideIn 0.3s ease;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.notification.error {
  background-color: #ff4d4f;
  color: white;
}

.notification.success {
  background-color: #52c41a;
  color: white;
}

.notification.info {
  background-color: #1890ff;
  color: white;
}
</style>
