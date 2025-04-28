<template>
  <div class="chat-room-container">
    <!-- 头部信息 -->
    <header class="chat-header">
      <div class="room-info">
        <h1>{{ roomName }}</h1>
        <div class="room-status">
          <span class="status-indicator" :class="{ 'connected': isConnected }"></span>
          <span class="status-text">{{ connectionStatus }}</span>
          <span class="online-count">在线用户: {{ onlineUsers.length }}</span>
        </div>
      </div>
      <div class="user-info">
        <button class="summary-button" @click="requestChatSummary" :disabled="!isConnected || summarizing">
          <i class="summary-icon"></i>
          {{ summarizing ? '正在总结...' : '总结聊天' }}
        </button>
        <span class="username">{{ username }}</span>
        <div class="avatar" v-if="userAvatar">
          <img :src="userAvatar" alt="用户头像" />
        </div>
        <div class="avatar default-avatar" v-else>
          {{ username.charAt(0).toUpperCase() }}
        </div>
        <button class="leave-button" @click="leaveRoom">退出聊天室</button>
      </div>
    </header>

    <!-- 主内容区 -->
    <main class="chat-main">
      <!-- 聊天消息区 -->
      <div class="messages-container" ref="messagesContainer">
        <div class="messages-wrapper">
          <div v-if="loadingHistory" class="loading-message">
            <div class="loading-spinner"></div>
            <span>加载历史消息中...</span>
          </div>
          
          <div v-for="(message, index) in messages" :key="message.messageId || index" 
               class="message-item" 
               :class="getMessageClass(message)">
            
            <!-- 日期分隔线 -->
            <div v-if="shouldShowDateSeparator(index)" class="date-separator">
              <span>{{ formatDate(message.sendTime) }}</span>
            </div>
            
            <!-- 系统消息 -->
            <div v-if="message.messageType === 'system'" class="system-message">
              <div class="system-message-content">
                <i class="system-icon"></i>
                <span>{{ message.content }}</span>
              </div>
              <div class="message-time">{{ formatTime(message.sendTime) }}</div>
            </div>
            
            <!-- 用户消息 -->
            <div v-else class="user-message" :class="{'self-message': message.senderId === userId}">
              <!-- 头像 -->
              <div class="message-avatar" v-if="message.senderId !== userId">
                <div class="avatar default-avatar">
                  {{ message.senderName.charAt(0).toUpperCase() }}
                </div>
              </div>
              
              <!-- 消息内容 -->
              <div class="message-content">
                <div class="message-info">
                  <span class="message-sender" v-if="message.senderId !== userId">{{ message.senderName }}</span>
                  <span class="message-time">{{ formatTime(message.sendTime) }}</span>
                </div>
                
                <!-- 文本消息 -->
                <div v-if="message.messageType === 'text'" class="message-text">
                  {{ message.content }}
                </div>
                
                <!-- 图片消息 -->
                <div v-else-if="message.messageType === 'image'" class="message-image">
                  <img :src="message.fileUrl" alt="图片消息" @click="previewImage(message.fileUrl)" />
                  <div class="image-info">{{ message.fileName }} ({{ formatFileSize(message.fileSize) }})</div>
                </div>
                
                <!-- 文件消息 -->
                <div v-else-if="message.messageType === 'file'" class="message-file" @click="downloadFile(message.fileUrl, message.fileName)">
                  <div class="file-icon"></div>
                  <div class="file-info">
                    <div class="file-name">{{ message.fileName }}</div>
                    <div class="file-size">{{ formatFileSize(message.fileSize) }}</div>
                  </div>
                  <div class="download-icon"></div>
                </div>
                
                <!-- 表情消息 -->
                <div v-else-if="message.messageType === 'emoji'" class="message-emoji">
                  {{ message.content }}
                </div>
              </div>
              
              <!-- 右侧头像(自己的消息) -->
              <div class="message-avatar self-avatar" v-if="message.senderId === userId">
                <div class="avatar default-avatar">
                  {{ username.charAt(0).toUpperCase() }}
                </div>
              </div>
            </div>
          </div>
          
          <!-- 新消息提示 -->
          <div v-if="hasNewMessage && !isAtBottom" class="new-message-indicator" @click="scrollToBottom">
            <i class="arrow-down-icon"></i>
            <span>有新消息</span>
          </div>
        </div>
      </div>
      
      <!-- 用户列表侧边栏 -->
      <div class="sidebar">
        <div class="sidebar-header">
          <h2>在线用户 ({{ onlineUsers.length }})</h2>
        </div>
        <div class="user-list">
          <div v-for="user in onlineUsers" :key="user.id" class="user-item">
            <div class="user-avatar">
              <img v-if="user.avatarUrl" :src="user.avatarUrl" alt="用户头像" />
              <div v-else class="default-avatar">{{ user.username.charAt(0).toUpperCase() }}</div>
            </div>
            <div class="user-details">
              <div class="user-name">{{ user.username }}</div>
              <div class="user-status" :class="user.status">{{ user.status === 'online' ? '在线' : '离线' }}</div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- 底部输入区 -->
    <footer class="chat-footer">
      <!-- 工具栏 -->
      <div class="toolbar">
        <div class="tool-button emoji-button" @click="toggleEmojiPanel">
          <i class="emoji-icon"></i>
        </div>
        <div class="tool-button image-button">
          <input type="file" accept="image/*" ref="imageInput" @change="handleImageUpload" class="file-input" />
          <i class="image-icon"></i>
        </div>
        <div class="tool-button file-button">
          <input type="file" ref="fileInput" @change="handleFileUpload" class="file-input" />
          <i class="file-icon"></i>
        </div>
      </div>
      
      <!-- 输入框 -->
      <div class="input-container">
        <!-- 表情面板 -->
        <div v-if="showEmojiPanel" class="emoji-panel">
          <div v-for="emoji in emojis" :key="emoji" class="emoji-item" @click="insertEmoji(emoji)">
            {{ emoji }}
          </div>
        </div>
        
        <textarea 
          class="message-input" 
          v-model="messageText" 
          placeholder="输入消息..." 
          @keydown.enter.prevent="sendMessage"
          ref="messageInput"></textarea>
      </div>
      
      <!-- 发送按钮 -->
      <button class="send-button" @click="sendMessage" :disabled="!isConnected || !messageText.trim()">
        <i class="send-icon"></i>
        <span>发送</span>
      </button>
    </footer>

    <!-- 图片预览弹窗 -->
    <div v-if="previewImageUrl" class="image-preview-modal" @click="closeImagePreview">
      <div class="image-preview-content">
        <img :src="previewImageUrl" alt="图片预览" />
        <button class="close-preview" @click.stop="closeImagePreview">×</button>
      </div>
    </div>

    <!-- 提示信息 -->
    <div v-if="notification.show" class="notification" :class="notification.type">
      {{ notification.message }}
    </div>
    
    <!-- 聊天总结弹窗 -->
    <div v-if="showSummaryModal" class="summary-modal-overlay" @click="closeSummaryModal">
      <div class="summary-modal-content" @click.stop>
        <div class="summary-modal-header">
          <h3>聊天总结</h3>
          <button class="close-summary" @click.stop="closeSummaryModal">×</button>
        </div>
        <div class="summary-modal-body">
          <div v-if="summarizing" class="summary-loading">
            <div class="loading-spinner"></div>
            <p>AI正在分析聊天记录，请稍候...</p>
          </div>
          <div v-else-if="summaryError" class="summary-error">
            <p>{{ summaryError }}</p>
          </div>
          <div v-else class="summary-content">
            <div v-html="formattedSummary"></div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount, computed, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';

export default {
  name: 'ChatRoom',
  props: {
    id: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const route = useRoute();
    const router = useRouter();
    
    // 用户信息
    const userId = ref(parseInt(localStorage.getItem('userId') || '0'));
    const username = ref(localStorage.getItem('username') || '访客');
    const userAvatar = ref(localStorage.getItem('userAvatar') || '');
    
    // 聊天室信息
    const roomId = computed(() => parseInt(props.id));
    const roomName = ref('公共聊天室');
    
    // 连接状态
    const isConnected = ref(false);
    const connectionStatus = ref('正在连接...');
    const connection = ref(null);
    
    // 消息相关
    const messages = ref([]);
    const messageText = ref('');
    const loadingHistory = ref(true);
    const messagesContainer = ref(null);
    const messageInput = ref(null);
    const hasNewMessage = ref(false);
    const isAtBottom = ref(true);
    
    // 在线用户
    const onlineUsers = ref([]);
    
    // 表情相关
    const showEmojiPanel = ref(false);
    const emojis = ref(['😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣', '🥲', '☺️', '😊', '😇', 
                      '🙂', '🙃', '😉', '😌', '😍', '🥰', '😘', '😗', '😙', '😚', '😋', '😛', 
                      '😝', '😜', '🤪', '🤨', '🧐', '🤓', '😎', '🥸', '🤩', '🥳', '😏', '😒']);
    
    // 文件上传相关
    const imageInput = ref(null);
    const fileInput = ref(null);
    
    // 图片预览
    const previewImageUrl = ref(null);
    
    // 通知提示
    const notification = ref({
      show: false,
      message: '',
      type: 'info',
      timeout: null
    });
    
    // 聊天总结相关
    const summarizing = ref(false);
    const chatSummary = ref('');
    const summaryError = ref('');
    const showSummaryModal = ref(false);

    // 创建SignalR连接
    const createConnection = () => {
      // 定义API基础URL为一个常量，便于统一修改
      const apiBaseUrl = 'http://localhost:5067'; // 正确的API地址
      
      // 创建新的连接
      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(`${apiBaseUrl}/chatHub`)
        .withAutomaticReconnect([0, 2000, 10000, 30000]) // 重连策略
        .configureLogging(signalR.LogLevel.Information)
        .build();
      
      // 存储API基础URL以供其他函数使用
      window.apiBaseUrl = apiBaseUrl;
      
      // 连接状态变化监听
      connection.value.onreconnecting(() => {
        isConnected.value = false;
        connectionStatus.value = '正在重新连接...';
        showNotification('连接断开，正在尝试重连...', 'warning');
      });
      
      connection.value.onreconnected(() => {
        isConnected.value = true;
        connectionStatus.value = '已连接';
        showNotification('已重新连接到聊天室', 'success');
        
        // 重新加入聊天室
        joinChatRoom();
      });
      
      connection.value.onclose(() => {
        isConnected.value = false;
        connectionStatus.value = '连接已关闭';
        showNotification('连接已关闭', 'error');
      });
      
      // 注册接收消息的处理函数
      registerSignalRHandlers();
      
      // 启动连接
      startConnection();
    };
    
    // 注册SignalR处理函数
    const registerSignalRHandlers = () => {
      // 接收新消息
      connection.value.on('ReceiveMessage', (message) => {
        console.log('收到新消息:', message);
        
        // 如果是系统消息，直接添加
        messages.value.push(message);
        
        // 检查是否需要滚动到底部
        if (isAtBottom.value) {
          nextTick(() => scrollToBottom());
        } else {
          hasNewMessage.value = true;
        }
        
        // 如果是其他用户发送的消息，且不是系统消息，显示消息通知
        if (message.senderId !== userId.value && message.messageType !== 'system') {
          showNotification(`${message.senderName}: ${message.messageType === 'text' ? message.content : '[' + message.messageType + '消息]'}`, 'info');
        }
      });
      
      // 接收历史消息
      connection.value.on('ReceiveHistoryMessages', (historyMessages) => {
        console.log('收到历史消息:', historyMessages);
        loadingHistory.value = false;
        
        if (historyMessages && historyMessages.length > 0) {
          // 将历史消息添加到消息列表
          messages.value = historyMessages.sort((a, b) => 
            new Date(a.sendTime) - new Date(b.sendTime)
          );
          
          // 滚动到底部
          nextTick(() => scrollToBottom());
        }
      });
      
      // 更新在线用户列表
      connection.value.on('UpdateOnlineUsers', (users) => {
        console.log('更新在线用户列表:', users);
        onlineUsers.value = users;
      });
      
      // 接收错误消息
      connection.value.on('Error', (errorMessage) => {
        console.error('SignalR错误:', errorMessage);
        showNotification(errorMessage, 'error');
      });
    };
    
    // 启动连接
    const startConnection = async () => {
      try {
        connectionStatus.value = '正在连接...';
        await connection.value.start();
        isConnected.value = true;
        connectionStatus.value = '已连接';
        console.log('SignalR连接已建立');
        
        // 连接成功后加入聊天室
        joinChatRoom();
      } catch (error) {
        console.error('连接SignalR失败:', error);
        connectionStatus.value = '连接失败';
        isConnected.value = false;
        
        // 5秒后重试
        setTimeout(startConnection, 5000);
      }
    };
    
    // 加入聊天室
    const joinChatRoom = async () => {
      if (!isConnected.value) {
        showNotification('尚未连接到服务器', 'error');
        return;
      }
      
      try {
        // 调用后端方法加入聊天室
        await connection.value.invoke('JoinChatRoom', userId.value, username.value, roomId.value);
        console.log(`成功加入聊天室 ${roomId.value}`);
      } catch (error) {
        console.error('加入聊天室失败:', error);
        showNotification('加入聊天室失败: ' + error, 'error');
      }
    };
    
    // 发送文本消息
    const sendMessage = async () => {
      if (!isConnected.value) {
        showNotification('未连接到服务器，无法发送消息', 'error');
        return;
      }
      
      if (!messageText.value.trim()) {
        return;
      }
      
      try {
        // 发送消息
        await connection.value.invoke('SendMessageToRoom', messageText.value);
        console.log('消息已发送');
        
        // 清空输入框
        messageText.value = '';
        
        // 让输入框重新获取焦点
        messageInput.value.focus();
      } catch (error) {
        console.error('发送消息失败:', error);
        showNotification('发送消息失败: ' + error, 'error');
      }
    };
    
    // 修改表情点击函数
    const insertEmoji = (emoji) => {
      // 将表情添加到输入框，而不是直接发送
      messageText.value += emoji;
      // 聚焦输入框
      nextTick(() => {
        messageInput.value.focus();
      });
      // 关闭表情面板
      showEmojiPanel.value = false;
    };
    
    // 修改上传图片函数
    const handleImageUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        // 显示上传中提示
        showNotification('图片上传中...', 'info');
        
        // 使用统一的API基础URL
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        console.log('图片上传成功:', response.data);
        
        // 发送图片消息
        await connection.value.invoke(
          'SendImageToRoom', 
          response.data.url, 
          response.data.fileName, 
          response.data.fileSize
        );
        
        // 清空文件输入框，允许重复上传相同文件
        imageInput.value.value = '';
        
        showNotification('图片发送成功', 'success');
      } catch (error) {
        console.error('图片上传失败:', error);
        // 更详细的错误日志
        if (error.response) {
          console.error('错误状态:', error.response.status);
          console.error('错误数据:', error.response.data);
          console.error('错误头部:', error.response.headers);
        }
        showNotification('图片上传失败: ' + (error.response?.data || error.message), 'error');
        
        // 清空文件输入框
        imageInput.value.value = '';
      }
    };
    
    // 修改上传文件函数
    const handleFileUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        // 显示上传中提示
        showNotification('文件上传中...', 'info');
        
        // 使用统一的API基础URL
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        console.log('文件上传成功:', response.data);
        
        // 发送文件消息
        await connection.value.invoke(
          'SendFileToRoom', 
          response.data.url, 
          response.data.fileName, 
          response.data.fileSize
        );
        
        // 清空文件输入框，允许重复上传相同文件
        fileInput.value.value = '';
        
        showNotification('文件发送成功', 'success');
      } catch (error) {
        console.error('文件上传失败:', error);
        // 更详细的错误日志
        if (error.response) {
          console.error('错误状态:', error.response.status);
          console.error('错误数据:', error.response.data);
          console.error('错误头部:', error.response.headers);
        }
        showNotification('文件上传失败: ' + (error.response?.data || error.message), 'error');
        
        // 清空文件输入框
        fileInput.value.value = '';
      }
    };
    
    // 切换表情面板
    const toggleEmojiPanel = () => {
      showEmojiPanel.value = !showEmojiPanel.value;
    };
    
    // 图片预览
    const previewImage = (url) => {
      previewImageUrl.value = url;
    };
    
    // 关闭图片预览
    const closeImagePreview = () => {
      previewImageUrl.value = null;
    };
    
    // 下载文件
    const downloadFile = (url, fileName) => {
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.target = '_blank';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    };
    
    // 滚动到底部
    const scrollToBottom = () => {
      if (messagesContainer.value) {
        messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
        hasNewMessage.value = false;
      }
    };
    
    // 检查滚动位置
    const checkScrollPosition = () => {
      if (messagesContainer.value) {
        const { scrollTop, scrollHeight, clientHeight } = messagesContainer.value;
        isAtBottom.value = Math.abs(scrollHeight - scrollTop - clientHeight) < 50;
      }
    };
    
    // 退出聊天室
    const leaveRoom = () => {
      if (connection.value) {
        connection.value.stop();
      }
      router.push('/home');
    };
    
    // 显示通知
    const showNotification = (message, type = 'info') => {
      // 清除之前的定时器
      if (notification.value.timeout) {
        clearTimeout(notification.value.timeout);
      }
      
      // 设置新的通知
      notification.value = {
        show: true,
        message,
        type,
        timeout: setTimeout(() => {
          notification.value.show = false;
        }, 3000)
      };
    };
    
    // 格式化时间
    const formatTime = (dateString) => {
      const date = new Date(dateString);
      return date.toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
      });
    };
    
    // 格式化日期
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
    
    // 格式化文件大小
    const formatFileSize = (bytes) => {
      if (!bytes) return '未知大小';
      
      const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
      if (bytes === 0) return '0 B';
      
      const i = Math.floor(Math.log(bytes) / Math.log(1024));
      return (bytes / Math.pow(1024, i)).toFixed(2) + ' ' + sizes[i];
    };
    
    // 获取消息类名
    const getMessageClass = (message) => {
      const classes = [];
      
      if (message.messageType === 'system') {
        classes.push('system-message-container');
      } else {
        classes.push(message.senderId === userId.value ? 'self-message-container' : 'other-message-container');
      }
      
      return classes.join(' ');
    };
    
    // 判断是否显示日期分隔线
    const shouldShowDateSeparator = (index) => {
      if (index === 0) return true;
      
      const currentDate = new Date(messages.value[index].sendTime).setHours(0, 0, 0, 0);
      const prevDate = new Date(messages.value[index - 1].sendTime).setHours(0, 0, 0, 0);
      
      return currentDate !== prevDate;
    };
    
    // 请求聊天总结
    const requestChatSummary = async () => {
      if (!isConnected.value) {
        showNotification('未连接到服务器，无法生成总结', 'error');
        return;
      }
      
      summarizing.value = true;
      summaryError.value = '';
      chatSummary.value = '';
      showSummaryModal.value = true;
      
      try {
        const response = await axios.post(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/summarize`, {
          roomId: roomId.value,
          userId: userId.value,
          username: username.value,
          messageCount: 100 // 默认获取最近100条消息
        });
        
        if (response.data && response.data.success) {
          chatSummary.value = response.data.message;
        } else {
          summaryError.value = response.data?.error || '生成总结失败，请稍后重试';
        }
      } catch (error) {
        console.error('获取聊天总结失败:', error);
        summaryError.value = '获取聊天总结失败: ' + (error.response?.data?.error || error.message);
      } finally {
        summarizing.value = false;
      }
    };
    
    // 关闭总结弹窗
    const closeSummaryModal = () => {
      showSummaryModal.value = false;
    };
    
    // 格式化总结内容（处理换行和标记）
    const formattedSummary = computed(() => {
      if (!chatSummary.value) return '';
      
      // 将换行符转换为<br>
      let formatted = chatSummary.value.replace(/\n/g, '<br>');
      
      // 删除多余的标记符号如 #，###, #### 等
      formatted = formatted.replace(/^#+\s+/gm, '');
      formatted = formatted.replace(/\s*#+\s*/gm, '');
      
      // 将标题转换为HTML标题
      formatted = formatted.replace(/^聊天记录总结.*$/m, '<h2>聊天记录总结</h2>');
      formatted = formatted.replace(/^(主要话题|重要观点和信息|提出的问题|达成的共识或结论|补充观察)$/gm, '<h3>$1</h3>');
      
      // 将Markdown风格的列表转换为HTML列表
      formatted = formatted.replace(/^- (.*?)$/gm, '<li>$1</li>');
      
      // 包装列表项到无序列表中
      if (formatted.includes('<li>')) {
        let parts = formatted.split('<h3>');
        for (let i = 1; i < parts.length; i++) {
          const headingEnd = parts[i].indexOf('</h3>');
          if (headingEnd !== -1) {
            const afterHeading = parts[i].substring(headingEnd + 5);
            if (afterHeading.includes('<li>')) {
              const withList = parts[i].substring(0, headingEnd + 5) + 
                              '<ul>' + 
                              afterHeading.replace(/(<li>.*?<\/li>)+/g, match => match) + 
                              '</ul>';
              parts[i] = withList;
            }
          }
        }
        formatted = parts.join('<h3>');
      }
      
      // 将Markdown风格的粗体和斜体转换为HTML标签
      formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
      formatted = formatted.replace(/\*(.*?)\*/g, '<em>$1</em>');
      
      // 美化注释部分
      formatted = formatted.replace(/注：(.*?)$/gm, '<div class="summary-note"><strong>注：</strong>$1</div>');
      
      return formatted;
    });
    
    // 组件挂载时
    onMounted(() => {
      // 如果用户未登录，重定向到登录页
      if (!userId.value || !username.value) {
        router.push('/login');
        return;
      }
      
      // 创建SignalR连接
      createConnection();
      
      // 监听滚动事件
      if (messagesContainer.value) {
        messagesContainer.value.addEventListener('scroll', checkScrollPosition);
      }
      
      // 点击其他地方隐藏表情面板
      document.addEventListener('click', (event) => {
        const emojiButton = document.querySelector('.emoji-button');
        const emojiPanel = document.querySelector('.emoji-panel');
        
        if (emojiButton && emojiPanel && 
            !emojiButton.contains(event.target) && 
            !emojiPanel.contains(event.target)) {
          showEmojiPanel.value = false;
        }
      });
    });
    
    // 组件卸载前
    onBeforeUnmount(() => {
      // 停止SignalR连接
      if (connection.value) {
        connection.value.stop();
      }
      
      // 移除滚动事件监听
      if (messagesContainer.value) {
        messagesContainer.value.removeEventListener('scroll', checkScrollPosition);
      }
      
      // 移除其他事件监听
      document.removeEventListener('click', () => {});
      
      // 清除定时器
      if (notification.value.timeout) {
        clearTimeout(notification.value.timeout);
      }
    });
    
    // 监听消息列表变化
    watch(messages, () => {
      if (isAtBottom.value) {
        nextTick(() => scrollToBottom());
      }
    });
    
    return {
      // 用户信息
      userId,
      username,
      userAvatar,
      
      // 聊天室信息
      roomId,
      roomName,
      
      // 连接状态
      isConnected,
      connectionStatus,
      
      // 消息相关
      messages,
      messageText,
      loadingHistory,
      messagesContainer,
      messageInput,
      hasNewMessage,
      isAtBottom,
      
      // 在线用户
      onlineUsers,
      
      // 表情相关
      showEmojiPanel,
      emojis,
      
      // 文件上传相关
      imageInput,
      fileInput,
      
      // 图片预览
      previewImageUrl,
      
      // 通知
      notification,
      
      // 聊天总结相关
      summarizing,
      chatSummary,
      summaryError,
      showSummaryModal,
      formattedSummary,
      
      // 方法
      sendMessage,
      insertEmoji,
      handleImageUpload,
      handleFileUpload,
      toggleEmojiPanel,
      previewImage,
      closeImagePreview,
      downloadFile,
      scrollToBottom,
      leaveRoom,
      formatTime,
      formatDate,
      formatFileSize,
      getMessageClass,
      shouldShowDateSeparator,
      requestChatSummary,
      closeSummaryModal
    };
  }
};
</script>

<style scoped>
/* 主容器 */
.chat-room-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fa;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
  color: #333;
  overflow: hidden;
  position: relative;
}

/* 头部样式 */
.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background-color: #ffffff;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
  z-index: 10;
}

.room-info {
  display: flex;
  flex-direction: column;
}

.room-info h1 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.room-status {
  display: flex;
  align-items: center;
  margin-top: 5px;
  font-size: 12px;
  color: #666;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background-color: #ff4d4f;
  margin-right: 6px;
  transition: background-color 0.3s ease;
}

.status-indicator.connected {
  background-color: #52c41a;
}

.status-text {
  margin-right: 10px;
}

.online-count {
  font-weight: 500;
  color: #1677ff;
}

.user-info {
  display: flex;
  align-items: center;
}

.username {
  margin-right: 10px;
  font-weight: 500;
}

.avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
  margin-right: 10px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.default-avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1677ff, #69c0ff);
  color: white;
  font-weight: bold;
  font-size: 16px;
}

.leave-button {
  padding: 6px 12px;
  background-color: #ff4d4f;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.3s ease;
}

.leave-button:hover {
  background-color: #ff7875;
}

/* 主内容区 */
.chat-main {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* 消息容器 */
.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  position: relative;
}

.messages-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100%;
}

.loading-message {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 15px;
  color: #666;
  font-size: 14px;
}

.loading-spinner {
  width: 20px;
  height: 20px;
  border: 2px solid rgba(0, 0, 0, 0.1);
  border-top-color: #1677ff;
  border-radius: 50%;
  margin-right: 10px;
  animation: spinner 0.8s linear infinite;
}

@keyframes spinner {
  to {
    transform: rotate(360deg);
  }
}

/* 日期分隔线 */
.date-separator {
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 15px 0;
  color: #999;
  font-size: 12px;
}

.date-separator span {
  padding: 2px 10px;
  background-color: #f0f2f5;
  border-radius: 10px;
}

/* 消息样式 */
.message-item {
  display: flex;
  flex-direction: column;
  margin-bottom: 15px;
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 系统消息 */
.system-message-container {
  align-items: center;
}

.system-message {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.system-message-content {
  display: flex;
  align-items: center;
  background-color: rgba(0, 0, 0, 0.03);
  padding: 6px 12px;
  border-radius: 15px;
  font-size: 12px;
  color: #999;
}

.system-icon {
  display: inline-block;
  width: 14px;
  height: 14px;
  background-color: #dadada;
  border-radius: 50%;
  margin-right: 6px;
}

/* 用户消息 */
.user-message {
  display: flex;
  margin-bottom: 20px;
}

.self-message-container .user-message {
  flex-direction: row-reverse;
}

.message-avatar {
  width: 40px;
  height: 40px;
  margin: 0 10px;
  flex-shrink: 0;
}

.message-content {
  max-width: 60%;
  display: flex;
  flex-direction: column;
}

.self-message-container .message-content {
  align-items: flex-end;
}

.message-info {
  margin-bottom: 5px;
  font-size: 12px;
  color: #999;
}

.message-sender {
  font-weight: 500;
  color: #666;
  margin-right: 8px;
}

.message-time {
  font-size: 12px;
  color: #bbb;
}

.self-message-container .message-time {
  text-align: right;
}

/* 文本消息 */
.message-text {
  padding: 10px 15px;
  border-radius: 6px;
  font-size: 14px;
  word-break: break-word;
  line-height: 1.5;
  position: relative;
}

.other-message-container .message-text {
  background-color: #ffffff;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  border-top-left-radius: 0;
}

.self-message-container .message-text {
  background-color: #1677ff;
  color: white;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  border-top-right-radius: 0;
}

/* 图片消息 */
.message-image {
  max-width: 250px;
  overflow: hidden;
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: transform 0.3s ease;
  cursor: pointer;
}

.message-image img {
  width: 100%;
  display: block;
  border-radius: 6px;
}

.message-image:hover {
  transform: scale(1.02);
}

.image-info {
  padding: 5px 10px;
  font-size: 12px;
  color: #999;
  background-color: rgba(255, 255, 255, 0.8);
  text-align: center;
}

/* 文件消息 */
.message-file {
  display: flex;
  align-items: center;
  background-color: #ffffff;
  padding: 10px;
  border-radius: 6px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.message-file:hover {
  background-color: #f5f7fa;
}

.file-icon {
  width: 32px;
  height: 40px;
  background-color: #e6f7ff;
  border-radius: 4px;
  margin-right: 10px;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.file-icon::before {
  content: "";
  display: block;
  width: 15px;
  height: 15px;
  background-color: #1677ff;
  clip-path: polygon(0% 0%, 70% 0%, 100% 30%, 100% 100%, 0% 100%);
}

.file-info {
  flex: 1;
  min-width: 0;
}

.file-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.file-size {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.download-icon {
  width: 20px;
  height: 20px;
  margin-left: 10px;
  position: relative;
}

.download-icon::before {
  content: "";
  display: block;
  width: 12px;
  height: 12px;
  border: 2px solid #1677ff;
  border-top: none;
  border-left: none;
  transform: rotate(45deg);
  position: absolute;
  top: 0;
  left: 4px;
}

.download-icon::after {
  content: "";
  display: block;
  width: 2px;
  height: 14px;
  background-color: #1677ff;
  position: absolute;
  top: 0;
  left: 9px;
}

/* 表情消息 */
.message-emoji {
  font-size: 24px;
  padding: 10px;
  background-color: transparent;
}

/* 新消息提示 */
.new-message-indicator {
  position: fixed;
  bottom: 100px;
  left: 50%;
  transform: translateX(-50%);
  background-color: #1677ff;
  color: white;
  padding: 8px 16px;
  border-radius: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  display: flex;
  align-items: center;
  cursor: pointer;
  animation: bounce 1s infinite alternate;
  z-index: 5;
}

@keyframes bounce {
  from {
    transform: translateX(-50%) translateY(0);
  }
  to {
    transform: translateX(-50%) translateY(-5px);
  }
}

.arrow-down-icon {
  width: 0;
  height: 0;
  border-left: 6px solid transparent;
  border-right: 6px solid transparent;
  border-top: 6px solid white;
  margin-right: 8px;
}

/* 侧边栏 */
.sidebar {
  width: 240px;
  background-color: #ffffff;
  border-left: 1px solid #eee;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: width 0.3s ease;
}

.sidebar-header {
  padding: 15px;
  border-bottom: 1px solid #eee;
}

.sidebar-header h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.user-list {
  flex: 1;
  overflow-y: auto;
  padding: 10px 0;
}

.user-item {
  display: flex;
  align-items: center;
  padding: 10px 15px;
  transition: background-color 0.3s ease;
  cursor: pointer;
}

.user-item:hover {
  background-color: #f5f7fa;
}

.user-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
  margin-right: 10px;
}

.user-details {
  flex: 1;
  min-width: 0;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-status {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.user-status.online {
  color: #52c41a;
}

/* 底部输入区 */
.chat-footer {
  padding: 15px 20px;
  background-color: #ffffff;
  border-top: 1px solid #eee;
  display: flex;
  align-items: flex-end;
  z-index: 10;
}

.toolbar {
  display: flex;
  margin-right: 10px;
  padding-bottom: 8px;
}

.tool-button {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 15px;
  cursor: pointer;
  position: relative;
  border-radius: 6px;
  transition: all 0.3s ease;
  background-color: #f0f2f5;
}

.tool-button:hover {
  background-color: #e0e3e9;
  transform: translateY(-2px);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}

.emoji-icon, .image-icon, .file-icon {
  width: 24px;
  height: 24px;
  display: block;
}

.emoji-icon {
  background-color: #ffd666;
  border-radius: 50%;
  position: relative;
  box-shadow: 0 2px 4px rgba(255, 214, 102, 0.3);
}

.emoji-icon::before {
  content: "";
  position: absolute;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: #fff;
  top: 7px;
  left: 7px;
}

.emoji-icon::after {
  content: "";
  position: absolute;
  width: 12px;
  height: 6px;
  border: 2px solid #fff;
  border-top: none;
  border-radius: 0 0 10px 10px;
  bottom: 5px;
  left: 6px;
}

.image-icon {
  background-color: #91d5ff;
  border-radius: 4px;
  position: relative;
  box-shadow: 0 2px 4px rgba(145, 213, 255, 0.3);
}

.image-icon::before {
  content: "";
  position: absolute;
  width: 18px;
  height: 14px;
  background-color: #fff;
  border-radius: 2px;
  top: 5px;
  left: 3px;
}

.image-icon::after {
  content: "";
  position: absolute;
  width: 6px;
  height: 6px;
  background-color: #91d5ff;
  border: 2px solid #fff;
  border-radius: 50%;
  top: 7px;
  left: 5px;
}

.file-icon {
  background-color: #b7eb8f;
  border-radius: 4px;
  position: relative;
  box-shadow: 0 2px 4px rgba(183, 235, 143, 0.3);
}

.file-icon::before {
  content: "";
  position: absolute;
  width: 14px;
  height: 18px;
  background-color: #fff;
  border-radius: 2px;
  top: 3px;
  left: 5px;
}

.file-icon::after {
  content: "";
  position: absolute;
  width: 8px;
  height: 2px;
  background-color: #b7eb8f;
  top: 8px;
  left: 8px;
  box-shadow: 0 3px 0 #b7eb8f, 0 6px 0 #b7eb8f;
}

.file-input {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  opacity: 0;
  cursor: pointer;
}

.input-container {
  flex: 1;
  position: relative;
}

.emoji-panel {
  position: absolute;
  bottom: 100%;
  left: 0;
  background-color: #ffffff;
  width: 280px;
  height: 200px;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  padding: 10px;
  display: flex;
  flex-wrap: wrap;
  overflow-y: auto;
  margin-bottom: 10px;
  z-index: 100;
  animation: fadeIn 0.2s ease;
}

.emoji-item {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 20px;
  border-radius: 4px;
  transition: background-color 0.2s ease;
}

.emoji-item:hover {
  background-color: #f5f7fa;
}

.message-input {
  width: 100%;
  min-height: 60px;
  max-height: 120px;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 10px 15px;
  font-size: 14px;
  resize: none;
  line-height: 1.5;
  outline: none;
  transition: border-color 0.3s ease, box-shadow 0.3s ease;
}

.message-input:focus {
  border-color: #1677ff;
  box-shadow: 0 0 0 2px rgba(22, 119, 255, 0.1);
}

.send-button {
  margin-left: 15px;
  padding: 0 20px;
  height: 36px;
  background-color: #1677ff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  transition: background-color 0.3s ease;
}

.send-button:hover {
  background-color: #4096ff;
}

.send-button:disabled {
  background-color: #d9d9d9;
  cursor: not-allowed;
}

.send-icon {
  margin-right: 6px;
  width: 16px;
  height: 16px;
  position: relative;
  display: inline-block;
}

.send-icon::before {
  content: "";
  position: absolute;
  top: 8px;
  left: 0;
  width: 14px;
  height: 1px;
  background-color: white;
}

.send-icon::after {
  content: "";
  position: absolute;
  top: 4px;
  left: 8px;
  width: 8px;
  height: 8px;
  border: 1px solid white;
  border-left: none;
  border-bottom: none;
  transform: rotate(45deg);
}

/* 图片预览弹窗 */
.image-preview-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  animation: fadeIn 0.3s ease;
}

.image-preview-content {
  position: relative;
  max-width: 90%;
  max-height: 90%;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.image-preview-content img {
  max-width: 100%;
  max-height: 80vh;
  display: block;
}

.close-preview {
  position: absolute;
  top: -20px;
  right: -20px;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: rgba(0, 0, 0, 0.5);
  color: white;
  border: none;
  font-size: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.close-preview:hover {
  background-color: rgba(0, 0, 0, 0.8);
}

/* 通知提示 */
.notification {
  position: fixed;
  top: 20px;
  left: 50%;
  transform: translateX(-50%);
  padding: 10px 20px;
  border-radius: 4px;
  color: white;
  font-size: 14px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  z-index: 1000;
  animation: slideDown 0.3s ease, fadeOut 0.3s ease 2.7s;
}

@keyframes slideDown {
  from {
    transform: translateX(-50%) translateY(-20px);
    opacity: 0;
  }
  to {
    transform: translateX(-50%) translateY(0);
    opacity: 1;
  }
}

@keyframes fadeOut {
  from {
    opacity: 1;
  }
  to {
    opacity: 0;
  }
}

.notification.info {
  background-color: #1677ff;
}

.notification.success {
  background-color: #52c41a;
}

.notification.warning {
  background-color: #faad14;
}

.notification.error {
  background-color: #ff4d4f;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .sidebar {
    position: absolute;
    right: 0;
    height: 100%;
    transform: translateX(100%);
    transition: transform 0.3s ease;
    z-index: 15;
  }
  
  .sidebar.active {
    transform: translateX(0);
  }
  
  .message-content {
    max-width: 80%;
  }
}

/* 聊天总结按钮 */
.summary-button {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  border-radius: 6px;
  padding: 8px 14px;
  font-size: 14px;
  margin-right: 15px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.summary-button:hover:not(:disabled) {
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.summary-button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.summary-icon {
  display: inline-block;
  width: 16px;
  height: 16px;
  margin-right: 6px;
  position: relative;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='white'%3E%3Cpath d='M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm-2 17H8v-2h4v2zm6-4H6v-2h12v2zm0-4H6v-2h12v2z'/%3E%3C/svg%3E");
  background-size: contain;
  background-repeat: no-repeat;
}

/* 聊天总结弹窗 */
.summary-modal-overlay {
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
  animation: fadeIn 0.3s ease;
}

.summary-modal-content {
  width: 700px;
  max-width: 90vw;
  max-height: 80vh;
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  display: flex;
  flex-direction: column;
  animation: scaleIn 0.3s ease;
  overflow: hidden;
}

.summary-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #eaeaea;
}

.summary-modal-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.close-summary {
  background: none;
  border: none;
  color: #999;
  font-size: 24px;
  cursor: pointer;
  line-height: 24px;
  padding: 0;
  margin: 0;
  transition: color 0.2s;
}

.close-summary:hover {
  color: #333;
}

.summary-modal-body {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
}

.summary-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
}

.summary-loading .loading-spinner {
  width: 40px;
  height: 40px;
  margin-bottom: 20px;
}

.summary-loading p {
  color: #666;
  text-align: center;
}

.summary-error {
  padding: 20px;
  background-color: #fff5f5;
  border-radius: 8px;
  border-left: 4px solid #ff4d4f;
  color: #cf1322;
}

.summary-content {
  color: #333;
  line-height: 1.6;
}

.summary-content h2 {
  margin-top: 0;
  font-size: 22px;
  color: #1677ff;
  margin-bottom: 20px;
  text-align: center;
  font-weight: 600;
  border-bottom: 1px solid #eaeaea;
  padding-bottom: 15px;
}

.summary-content h3 {
  font-size: 18px;
  color: #333;
  margin-top: 20px;
  margin-bottom: 12px;
  font-weight: 600;
  border-left: 4px solid #1677ff;
  padding-left: 10px;
  background-color: #f0f7ff;
  padding: 8px 12px;
  border-radius: 0 4px 4px 0;
}

.summary-content ul {
  padding-left: 20px;
  margin-bottom: 20px;
  background-color: #f9f9f9;
  border-radius: 6px;
  padding: 15px 20px 15px 35px;
}

.summary-content li {
  margin-bottom: 10px;
  line-height: 1.5;
  position: relative;
}

.summary-content li::before {
  content: "";
  position: absolute;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background-color: #1677ff;
  left: -15px;
  top: 8px;
}

.summary-note {
  margin-top: 20px;
  padding: 12px 15px;
  background-color: #fffbe6;
  border-left: 4px solid #faad14;
  border-radius: 0 4px 4px 0;
  color: #876800;
  font-size: 14px;
}

/* 动画 */
@keyframes scaleIn {
  from {
    opacity: 0;
    transform: scale(0.9);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

/* 响应式 */
@media (max-width: 768px) {
  .summary-modal-content {
    width: 95vw;
    max-height: 85vh;
  }
}

.chat-actions {
  display: flex;
  gap: 0.5rem;
}
</style> 