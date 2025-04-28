<template>
  <div class="ai-sidebar" :class="{ 'expanded': isExpanded }">
    <div class="ai-header">
      <div class="ai-title">
        <div class="ai-avatar">
          <span>AI</span>
        </div>
        <h3>WHU-AI助手</h3>
      </div>
      <button class="toggle-button" @click="toggleSidebar">
        <i class="toggle-icon"></i>
      </button>
    </div>
    
    <div class="ai-messages" ref="messagesContainer">
      <div v-if="messages.length === 0" class="welcome-message">
        <div class="welcome-avatar">AI</div>
        <div class="welcome-text">
          <h4>你好，我是WHU-AI助手</h4>
          <p>我可以回答关于武汉大学的问题，也可以帮助你解决学习和生活中的困惑。请问有什么我可以帮你的吗？</p>
        </div>
      </div>
      
      <div v-for="(message, index) in messages" :key="index" 
           class="message" 
           :class="{ 'user-message': message.role === 'user', 'ai-message': message.role === 'assistant' }">
        <div class="message-avatar" v-if="message.role === 'assistant'">AI</div>
        <div class="message-content">
          <div class="message-text" v-html="formatMessage(message.content)"></div>
          <div class="message-time">{{ formatTime(message.timestamp) }}</div>
        </div>
        <div class="message-avatar user-avatar" v-if="message.role === 'user'">
          {{ username.charAt(0).toUpperCase() }}
        </div>
      </div>
      
      <div v-if="loading" class="ai-typing">
        <div class="typing-indicator">
          <span></span>
          <span></span>
          <span></span>
        </div>
        <div class="typing-text">AI正在思考...</div>
      </div>
    </div>
    
    <div class="ai-input">
      <textarea 
        class="message-input" 
        v-model="messageText" 
        placeholder="输入问题..." 
        @keydown.enter.prevent="sendMessage"
        :disabled="loading"
        ref="messageInput"></textarea>
      <button class="send-button" @click="sendMessage" :disabled="loading || !messageText.trim()">
        <i class="send-icon"></i>
      </button>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, nextTick, watch } from 'vue';
import axios from 'axios';

export default {
  name: 'AISidebar',
  props: {
    userId: {
      type: Number,
      required: true
    },
    username: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const isExpanded = ref(false);
    const messages = ref([]);
    const messageText = ref('');
    const loading = ref(false);
    const messagesContainer = ref(null);
    const messageInput = ref(null);
    
    // 切换侧边栏
    const toggleSidebar = () => {
      isExpanded.value = !isExpanded.value;
    };
    
    // 发送消息
    const sendMessage = async () => {
      if (loading.value || !messageText.value.trim()) {
        return;
      }
      
      const userMessage = {
        role: 'user',
        content: messageText.value,
        timestamp: new Date()
      };
      
      // 添加用户消息
      messages.value.push(userMessage);
      
      // 清空输入框
      const userMessageText = messageText.value;
      messageText.value = '';
      
      // 滚动到底部
      await nextTick();
      scrollToBottom();
      
      // 设置加载状态
      loading.value = true;
      
      try {
        // 创建历史消息数组（只传递最近10条消息，避免过长）
        const recentMessages = messages.value.slice(-10).map(msg => ({
          role: msg.role,
          content: msg.content
        }));
        
        // 发送请求
        const response = await axios.post(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/chat`, {
          userId: props.userId,
          username: props.username,
          message: userMessageText,
          history: recentMessages
        });
        
        // 添加AI回复
        if (response.data && response.data.success) {
          messages.value.push({
            role: 'assistant',
            content: response.data.message,
            timestamp: new Date()
          });
        } else {
          // 处理错误
          messages.value.push({
            role: 'assistant',
            content: `抱歉，我遇到了一些问题：${response.data?.error || '未知错误'}`,
            timestamp: new Date()
          });
        }
      } catch (error) {
        console.error('AI请求失败:', error);
        // 添加错误消息
        messages.value.push({
          role: 'assistant',
          content: '抱歉，我暂时无法回应你的问题。请稍后再试。',
          timestamp: new Date()
        });
      } finally {
        // 取消加载状态
        loading.value = false;
        
        // 滚动到底部
        await nextTick();
        scrollToBottom();
      }
    };
    
    // 格式化消息（处理换行符和特殊标记）
    const formatMessage = (text) => {
      if (!text) return '';
      
      // 将换行符转换为<br>
      let formatted = text.replace(/\n/g, '<br>');
      
      // 将*text*转换为<strong>text</strong>
      formatted = formatted.replace(/\*([^*]+)\*/g, '<strong>$1</strong>');
      
      // 将_text_转换为<em>text</em>
      formatted = formatted.replace(/\_([^_]+)\_/g, '<em>$1</em>');
      
      return formatted;
    };
    
    // 格式化时间
    const formatTime = (timestamp) => {
      if (!timestamp) return '';
      
      const date = new Date(timestamp);
      return date.toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
      });
    };
    
    // 滚动到底部
    const scrollToBottom = () => {
      if (messagesContainer.value) {
        messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
      }
    };
    
    // 监听消息变化，滚动到底部
    watch(messages, () => {
      nextTick(() => scrollToBottom());
    });
    
    // 组件挂载时
    onMounted(() => {
      // 自动聚焦输入框
      if (messageInput.value) {
        messageInput.value.focus();
      }
    });
    
    return {
      isExpanded,
      messages,
      messageText,
      loading,
      messagesContainer,
      messageInput,
      toggleSidebar,
      sendMessage,
      formatMessage,
      formatTime,
      scrollToBottom,
      username: props.username
    };
  }
};
</script>

<style scoped>
.ai-sidebar {
  position: fixed;
  top: 0;
  right: 0;
  width: 70px;
  height: 100vh;
  background-color: #ffffff;
  box-shadow: -2px 0 10px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  transition: width 0.3s ease;
  z-index: 1000;
  overflow: hidden;
}

.ai-sidebar.expanded {
  width: 360px;
}

.ai-header {
  padding: 15px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #eee;
}

.ai-title {
  display: flex;
  align-items: center;
  opacity: 0;
  transition: opacity 0.3s ease;
  pointer-events: none;
}

.expanded .ai-title {
  opacity: 1;
  pointer-events: auto;
}

.ai-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 10px;
}

.ai-title h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.toggle-button {
  width: 40px;
  height: 40px;
  border: none;
  background: transparent;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.toggle-icon {
  width: 20px;
  height: 20px;
  position: relative;
}

.toggle-icon::before, .toggle-icon::after {
  content: "";
  position: absolute;
  background-color: #6366f1;
  transition: transform 0.3s ease;
}

.toggle-icon::before {
  width: 20px;
  height: 2px;
  top: 9px;
}

.toggle-icon::after {
  width: 2px;
  height: 20px;
  left: 9px;
}

.expanded .toggle-icon::after {
  transform: rotate(90deg);
}

.ai-messages {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
  display: none;
}

.expanded .ai-messages {
  display: block;
}

.welcome-message {
  display: flex;
  align-items: flex-start;
  padding: 10px;
  background-color: #f7f9ff;
  border-radius: 8px;
  margin-bottom: 15px;
}

.welcome-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 10px;
  flex-shrink: 0;
}

.welcome-text {
  flex: 1;
}

.welcome-text h4 {
  margin: 0 0 5px 0;
  font-size: 15px;
  color: #333;
}

.welcome-text p {
  margin: 0;
  font-size: 14px;
  color: #666;
  line-height: 1.5;
}

.message {
  display: flex;
  margin-bottom: 20px;
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

.user-message {
  flex-direction: row-reverse;
}

.message-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin: 0 10px;
  flex-shrink: 0;
}

.user-avatar {
  background: linear-gradient(135deg, #1677ff, #69c0ff);
}

.message-content {
  max-width: 70%;
  display: flex;
  flex-direction: column;
}

.user-message .message-content {
  align-items: flex-end;
}

.message-text {
  padding: 10px 15px;
  border-radius: 8px;
  font-size: 14px;
  line-height: 1.5;
  word-break: break-word;
}

.ai-message .message-text {
  background-color: #f7f9ff;
  color: #333;
  border-top-left-radius: 0;
}

.user-message .message-text {
  background-color: #1677ff;
  color: white;
  border-top-right-radius: 0;
}

.message-time {
  font-size: 12px;
  color: #aaa;
  margin-top: 5px;
}

.ai-typing {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
}

.typing-indicator {
  display: flex;
  margin-right: 10px;
}

.typing-indicator span {
  width: 8px;
  height: 8px;
  margin: 0 2px;
  background-color: #6366f1;
  border-radius: 50%;
  display: inline-block;
  animation: blink 1.4s infinite both;
}

.typing-indicator span:nth-child(2) {
  animation-delay: 0.2s;
}

.typing-indicator span:nth-child(3) {
  animation-delay: 0.4s;
}

@keyframes blink {
  0% {
    opacity: 0.2;
  }
  20% {
    opacity: 1;
  }
  100% {
    opacity: 0.2;
  }
}

.typing-text {
  font-size: 14px;
  color: #666;
}

.ai-input {
  padding: 15px;
  border-top: 1px solid #eee;
  display: none;
}

.expanded .ai-input {
  display: flex;
}

.message-input {
  flex: 1;
  height: 40px;
  max-height: 80px;
  border: 1px solid #ddd;
  border-radius: 20px;
  padding: 10px 15px;
  font-size: 14px;
  resize: none;
  outline: none;
  transition: border-color 0.3s ease;
}

.message-input:focus {
  border-color: #6366f1;
}

.send-button {
  width: 40px;
  height: 40px;
  border: none;
  background-color: #6366f1;
  color: white;
  border-radius: 50%;
  margin-left: 10px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background-color 0.3s ease;
}

.send-button:hover {
  background-color: #4f46e5;
}

.send-button:disabled {
  background-color: #d1d5db;
  cursor: not-allowed;
}

.send-icon {
  width: 16px;
  height: 16px;
  position: relative;
}

.send-icon::before {
  content: "";
  position: absolute;
  width: 16px;
  height: 2px;
  background-color: white;
  top: 7px;
  left: 0;
  transform: rotate(45deg);
}

.send-icon::after {
  content: "";
  position: absolute;
  width: 10px;
  height: 2px;
  background-color: white;
  top: 9px;
  left: 2px;
  transform: rotate(-45deg);
}
</style> 