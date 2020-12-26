<template>
  <q-page padding>
    <q-input
      v-model="fqdn"
      class="q-mb-sm"
      outlined
      label="Fully Qualified Domain Name (e.x. test.nager.at)"
    />

    <q-btn
      color="primary"
      label="Request a new certificate"
      @click="create"
    />
  </q-page>
</template>

<script>
export default {
  name: 'PageIndex',
  data () {
    return {
      fqdn: ''
    }
  },
  methods: {
    async create () {
      try {
        await this.$axios.post('/api/Certificate', { fqdn: this.fqdn })
        this.$emit('afterSubmit')
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    }
  }
}
</script>
